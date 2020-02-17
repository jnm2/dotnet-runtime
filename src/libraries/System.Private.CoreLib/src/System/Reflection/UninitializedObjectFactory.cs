// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Reflection
{
    public abstract class UninitializedObjectFactory
    {
        private protected UninitializedObjectFactory(RuntimeType targetType)
        {
            Debug.Assert(targetType != null);
            TargetType = targetType;
        }

        public Type TargetType { get; }

        public static UninitializedObjectFactory CreateFactory(Type type)
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

            RuntimeType closedFactoryType = (RuntimeType)typeof(UninitializedObjectFactory<>).MakeGenericType(rt);
            return (UninitializedObjectFactory)closedFactoryType.CreateInstanceDefaultCtor(publicOnly: false, skipCheckThis: false, fillCache: false, wrapExceptions: false)!;
        }

        public static UninitializedObjectFactory<T> CreateFactory<T>()
        {
            return new UninitializedObjectFactory<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? CreateUninitializedInstance()
        {
            return Unsafe.As<IInternalObjectFactory>(this).Invoke();
        }

        internal static (IntPtr typeHnd, IntPtr newobjFn) GetParameters(Type type)
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

            IntPtr typeHnd = rt.GetTypeHandleInternal().Value;
            IntPtr newobjFn = RuntimeHelpers.GetNewobjHelper(rt);

            return (typeHnd, newobjFn);
        }
    }

    public sealed class UninitializedObjectFactory<T> : UninitializedObjectFactory, IInternalObjectFactory
    {
        private readonly IntPtr _typeHnd;
        private readonly IntPtr _newobjFn;

        internal UninitializedObjectFactory()
            : base((RuntimeType)typeof(T))
        {
            if (!typeof(T).IsValueType)
            {
                (_typeHnd, _newobjFn) = GetParameters(typeof(T));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new T CreateUninitializedInstance()
        {
            if (typeof(T).IsValueType)
            {
                return default!;
            }
            else
            {
                T retVal = RuntimeHelpers.InvokeNewobjHelper<T>(_typeHnd, _newobjFn);
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
                object retVal = RuntimeHelpers.InvokeNewobjHelper<T>(_typeHnd, _newobjFn)!;
                GC.KeepAlive(this); // keeps 'T' instantiation alive
                return retVal;
            }
        }
    }
}
