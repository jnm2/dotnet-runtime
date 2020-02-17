// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public sealed class ObjectFactory<T> : IObjectFactory
    {
        private readonly IntPtr _typeHnd;
        private readonly IntPtr _newobjFn;
        private readonly IntPtr _ctorFn;

        public ObjectFactory()
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
        public T CreateObject()
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

        object? IObjectFactory.CreateObject()
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
    }

    public interface IObjectFactory
    {
        object? CreateObject();
    }
}
