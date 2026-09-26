using System;
using UnityEditor;
using UnityEngine;
namespace SkiStation.Editor
{
    public static class VisitorDecisionChecks329
    {
        [MenuItem("SkiStation/Visiteurs/Verifier les decisions 3.29")]
        public static void Run()
        {
            Check(VisitorDayAction329.Home, 33, 33, 20, .9f, .1f, true);
            Check(VisitorDayAction329.Rest, 120, 33, 20, .9f, .2f, true);
            Check(VisitorDayAction329.Eat, 120, 33, 20, .8f, .8f, true);
            Check(VisitorDayAction329.Ski, 120, 33, 20, .8f, .8f, false);
            Check(VisitorDayAction329.Home, 50, 33, 20, .1f, 1f, false);
            Check(VisitorDayAction329.Ski, 53, 33, 20, .1f, 1f, false);
            Check(VisitorDayAction329.Rest, 90, 33, float.PositiveInfinity, .1f, 1f, false);
            Check(VisitorDayAction329.Home, 40, 33, float.PositiveInfinity, .1f, 1f, false);
            Check(VisitorDayAction329.Rest, 90, 33, float.NaN, .1f, 1f, false);
            Debug.Log("PASS 3.29 : 9 cas de politique de decision. Ne valide pas les deplacements ni les services Unity.");
        }
        static void Check(VisitorDayAction329 expected, float left, float reserve, float trip, float hunger, float energy, bool meal)
        {
            var actual = VisitorDecisionRules329.Choose(left, reserve, trip, hunger, energy, meal);
            if (actual != expected) throw new Exception("Decision attendue " + expected + ", obtenue " + actual);
        }
    }
}
