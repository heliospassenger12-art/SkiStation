using UnityEngine;
namespace SkiStation {
 public class GroomingGarageRecommendationRuntime:MonoBehaviour {
  public float recommendedSearchRadius=400f; public int evaluatedSlopes; public float averageAccessDistance,coverageScore;
  public void Evaluate(Vector3 position){var slopes=FindObjectsOfType<SlopeRuntime>();evaluatedSlopes=0;float sum=0f;foreach(var s in slopes){if(s==null)continue;evaluatedSlopes++;sum+=Vector3.Distance(position,s.BottomPoint);}averageAccessDistance=evaluatedSlopes>0?sum/evaluatedSlopes:0f;coverageScore=evaluatedSlopes>0?Mathf.Clamp01(1f-averageAccessDistance/Mathf.Max(1f,recommendedSearchRadius)):0f;}
 }
}
