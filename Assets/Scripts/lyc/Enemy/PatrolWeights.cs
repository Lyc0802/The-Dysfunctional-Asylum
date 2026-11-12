using System;
using System.Collections.Generic;

namespace Asylum.Enemies
{
    public static class PatrolWeights
    {
        public static float Evaluate(float baseWeight, bool isVent, bool isForward, float ventMultiplier, float forwardMultiplier)
        {
            if(float.IsNaN(baseWeight) || float.IsInfinity(baseWeight) || baseWeight<=0) return 0;
            float multiplier=isVent ? ventMultiplier : isForward ? forwardMultiplier : 1;
            float result=baseWeight*Math.Max(0,multiplier);
            return float.IsNaN(result)||float.IsInfinity(result) ? 0 : result;
        }
        public static int Choose(IReadOnlyList<float> weights, double sample)
        {
            double total=0;
            for(int i=0;i<weights.Count;i++) if(weights[i]>0&&!float.IsInfinity(weights[i])&&!float.IsNaN(weights[i])) total+=weights[i];
            if(total<=0) return -1;
            double roll=Math.Max(0,Math.Min(1,sample))*total;
            int last=-1;
            for(int i=0;i<weights.Count;i++)
            {
                if(weights[i]<=0||float.IsInfinity(weights[i])||float.IsNaN(weights[i])) continue;
                last=i; roll-=weights[i]; if(roll<0) return i;
            }
            return last;
        }
    }
}
