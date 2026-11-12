using System;
using Asylum.Enemies;

public static class PatrolWeightChecks
{
    static int count;
    static void Check(bool value,string message) { if(!value) throw new Exception(message); count++; }
    public static void Main()
    {
        Check(PatrolWeights.Choose(new float[]{0,0},.5)==-1,"No valid edges must remain unselected");
        Check(PatrolWeights.Choose(new float[]{0,1,0},0)==1,"Zero-weight edge selected at lower boundary");
        Check(PatrolWeights.Choose(new float[]{0,1,0},1)==1,"Zero-weight edge selected at upper boundary");
        Check(PatrolWeights.Choose(new float[]{float.NaN,-1,float.PositiveInfinity,2},.1)==3,"Invalid weights must not poison selection");
        Check(PatrolWeights.Evaluate(0,false,true,1,100)==0,"Difficulty must not unlock adjacency-only edges");
        Check(PatrolWeights.Evaluate(3,true,false,0,5)==0,"Vent preference zero must disable vent entries");
        Check(PatrolWeights.Evaluate(3,true,false,2,5)==6,"Vent modifier must apply only once");
        Check(PatrolWeights.Evaluate(15,false,true,2,2)==30,"Forward bias incorrect");
        Check(PatrolWeights.Evaluate(5,false,false,2,2)==5,"Backward/same base weight changed");
        var random=new Random(42); var hits=new int[3];
        for(int i=0;i<100000;i++) hits[PatrolWeights.Choose(new float[]{5,3,2},random.NextDouble())]++;
        Check(Math.Abs(hits[0]/100000.0-.5)<.01,"50% branch distribution drift");
        Check(Math.Abs(hits[1]/100000.0-.3)<.01,"30% branch distribution drift");
        Check(Math.Abs(hits[2]/100000.0-.2)<.01,"20% branch distribution drift");
        Console.WriteLine("PASS "+count+" patrol weight checks, including 100000 seeded selections.");
    }
}
