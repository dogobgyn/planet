using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace TwoDee
{
    public interface IRandomGenerator
    {
        float Value
        {
            get;
        }
    }
    public static class RandomGeneratorExtensions
    {
        // Inclusive max
        public static float Range(this IRandomGenerator gen, float min, float max)
        {
            return min + (max - min) * gen.Value;
        }

        // Exclusive max
        public static int Range(this IRandomGenerator gen, int min, int max)
        {
            if (min >= max)
            {
                Debug.LogError("RandomGeneratorExtensions.Range error");
                return min;
            }
            int delta = (max - min);
            return min + Mathf.FloorToInt(gen.Range(0.0f, delta)) % delta;
        }

        public static void Shuffle<T>(this IRandomGenerator gen, List<T> list)
        {
            for(int i=0;i<list.Count-1;i++)
            {
                int indexToSwap = gen.Range(i + 1, list.Count);
                T temp = list[i];
                list[i] = list[indexToSwap];
                list[indexToSwap] = temp;
            }
        }
    }

    public class RandomGeneratorUnity : IRandomGenerator
    {
        UnityEngine.Random.State m_State;
        public float Value
        {
            get
            {
                UnityEngine.Random.state = m_State;
                float result = UnityEngine.Random.value;
                m_State = UnityEngine.Random.state;
                return result;
            }
        }

        public int IntValue
        {
            get
            {
                UnityEngine.Random.state = m_State;
                int result = UnityEngine.Random.Range(0, int.MaxValue);
                m_State = UnityEngine.Random.state;
                return result;
            }
        }

        public RandomGeneratorUnity(long seed)
        {
            UnityEngine.Random.InitState((int)seed);
            m_State = UnityEngine.Random.state;
        }
    }
}
