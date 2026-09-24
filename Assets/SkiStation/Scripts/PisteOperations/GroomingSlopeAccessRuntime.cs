namespace SkiStation {
 public static class GroomingSlopeAccessRuntime {
  public static bool HasActiveSkiers(SlopeRuntime slope)=>slope!=null&&slope.TrafficCount>0;
  public static bool CanCloseForGrooming(SlopeRuntime slope)=>slope!=null&&slope.TrafficCount<=0;
  public static void CloseForGrooming(SlopeRuntime slope){if(slope!=null)slope.SetOpen(false);}
  public static void ReopenAfterGrooming(SlopeRuntime slope){if(slope!=null)slope.SetOpen(true);}
 }
}
