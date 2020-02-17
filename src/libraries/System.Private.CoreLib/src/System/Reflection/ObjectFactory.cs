// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Reflection
{
    public abstract class ObjectFactory
    {
        private protected ObjectFactory(RuntimeType targetType)
        {
            Debug.Assert(targetType != null);
            TargetType = targetType;
        }

        public Type TargetType { get; }

        public static ObjectFactory CreateFactory(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!(type is RuntimeType rt))
            {
                throw new ArgumentException(
                    paramName: nameof(type),
                    message: SR.Argument_MustBeRuntimeType);
            }

            type = null!; // just to make sure we don't use Type for the rest of the method

            if (rt.IsPointer || rt.IsByRef || rt.IsByRefLike)
            {
                throw new ArgumentException(
                    paramName: nameof(type),
                    message: SR.NotSupported_Type);
            }

            RuntimeType closedFactoryType = (RuntimeType)typeof(ObjectFactory<>).MakeGenericType(rt);
            return (ObjectFactory)closedFactoryType.CreateInstanceDefaultCtor(publicOnly: false, skipCheckThis: false, fillCache: false, wrapExceptions: false)!;
        }

        public static ObjectFactory<T> CreateFactory<T>()
        {
            return new ObjectFactory<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? CreateInstance()
        {
            return Unsafe.As<IInternalObjectFactory>(this).Invoke();
        }
    }

    public sealed class ObjectFactory<T> : ObjectFactory, IInternalObjectFactory
    {
        private readonly IntPtr _typeHnd;
        private readonly IntPtr _newobjFn;
        private readonly IntPtr _ctorFn;

        internal ObjectFactory()
            : base((RuntimeType)typeof(T))
        {
            if (!typeof(T).IsValueType)
            {
                (_typeHnd, _newobjFn) = UninitializedObjectFactory.GetParameters(typeof(T));

                ConstructorInfo? ci = typeof(T).GetConstructor(Type.EmptyTypes);
                if (ci is null)
                {
                    throw new InvalidOperationException(); // TODO: replace with real error message
                }

                _ctorFn = ci.MethodHandle.GetFunctionPointer();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new T CreateInstance()
        {
            if (typeof(T).IsValueType)
            {
                return default!;
            }
            else
            {
                T retVal = RuntimeHelpers.InvokeNewobjHelper<T>(_typeHnd, _newobjFn);
                RuntimeHelpers.InvokeCtor(retVal!, _ctorFn); // call parameterless ctor
                GC.KeepAlive(this); // keeps 'T' instantiation alive
                return retVal;
            }
        }

        object? IInternalObjectFactory.Invoke()
        {
            if (typeof(T).IsValueType)
            {
                return default(T);
            }
            else
            {
                T retVal = RuntimeHelpers.InvokeNewobjHelper<T>(_typeHnd, _newobjFn);
                RuntimeHelpers.InvokeCtor(retVal!, _ctorFn); // call parameterless ctor
                GC.KeepAlive(this); // keeps 'T' instantiation alive
                return retVal;
            }
        }
    }

    internal interface IInternalObjectFactory
    {
        object? Invoke();
    }
}
