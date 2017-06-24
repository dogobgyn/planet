
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace TwoDee
{
    public static class Profiler
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Begin()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            var str = sf.GetMethod().Name;
            UnityEngine.Profiling.Profiler.BeginSample(str);
        }
        public static void Begin(object ob)
        {
            UnityEngine.Profiling.Profiler.BeginSample(ob.GetType().Name);
        }
        public static void End()
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}