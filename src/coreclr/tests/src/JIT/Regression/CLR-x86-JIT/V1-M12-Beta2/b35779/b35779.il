// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x77e65197 {}
.class ILGEN_0x77e65197 {
.method static int32 Main() {
.entrypoint
.maxstack	20
 .locals (float64 local_0x2)
.try {
 ldc.r8 2.0e+019
 stloc local_0x2

 ldloc local_0x2
 conv.u8
 conv.ovf.u8
 conv.i4
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
 ret
}
}
