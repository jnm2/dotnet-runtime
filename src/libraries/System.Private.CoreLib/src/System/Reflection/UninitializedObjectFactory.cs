// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Reflection
{
    public sealed class UninitializedObjectFactory<T> : IUninitializedObjectFactory where T : class
    {
        private readonly IntPtr _typeHnd;
        private readonly IntPtr _newobjFn;

        public UninitializedObjectFactory()
        {
            // TODO: Make sure T isn't array or string
            // TODO: Special-case reference type T vs. value type T

            RuntimeType rt = typeof(T) as RuntimeType ?? throw new NotSupportedException();

            RuntimeHelpers.RunClassConstructor(rt.GetTypeHandleInternal());
            _typeHnd = rt.GetTypeHandleInternal().Value;
            _newobjFn = RuntimeHelpers.GetNewobjHelper(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateUninitializedObject()
        {
            T retVal = Unsafe.As<T>(RuntimeHelpers.InvokeNewobjHelper(_typeHnd, _newobjFn));
            GC.KeepAlive(this); // keeps 'T' instantiation alive
            return retVal;
        }

        object? IUninitializedObjectFactory.CreateUninitializedObject()
        {
            object retVal = RuntimeHelpers.InvokeNewobjHelper(_typeHnd, _newobjFn);
            GC.KeepAlive(this); // keeps 'T' instantiation alive
            return retVal;
        }
    }

    public interface IUninitializedObjectFactory
    {
        object? CreateUninitializedObject();
    }
}
