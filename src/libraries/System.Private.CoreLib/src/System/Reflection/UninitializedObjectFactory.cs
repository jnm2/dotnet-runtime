// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class UninitializedObjectFactory
    {
        public static (IntPtr typeHnd, IntPtr newobjFn) GetParameters(Type type)
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

    public sealed class UninitializedObjectFactory<T> : IUninitializedObjectFactory
    {
        private readonly IntPtr _typeHnd;
        private readonly IntPtr _newobjFn;

        public UninitializedObjectFactory()
        {
            if (!typeof(T).IsValueType)
            {
                (_typeHnd, _newobjFn) = UninitializedObjectFactory.GetParameters(typeof(T));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateUninitializedObject()
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

        object? IUninitializedObjectFactory.CreateUninitializedObject()
        {
            if (typeof(T).IsValueType)
            {
                return default!;
            }
            else
            {
                object retVal = RuntimeHelpers.InvokeNewobjHelper<T>(_typeHnd, _newobjFn)!;
                GC.KeepAlive(this); // keeps 'T' instantiation alive
                return retVal;
            }
        }
    }

    public interface IUninitializedObjectFactory
    {
        object? CreateUninitializedObject();
    }
}
