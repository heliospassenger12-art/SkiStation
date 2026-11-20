using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed partial class SlopeConstructionMode
    {
        // Test-scene input adapter. Same AddPoint, Finish, recap, cost and confirmation
        // as mouse authoring. It never builds, pays, opens or publishes a piste.
        public bool PrepareValAlpinReturnQuote(IReadOnlyList<Vector3> controls,out string reason)
        {
            reason="Ouvrir ValAlpin en Play Mode, hors session Arcade.";
            if(!Application.isPlaying||!ValAlpin.ValAlpinScene.IsActive||Arcade.ArcadeSceneIsolation.IsActive||Arcade.ArcadeSkiSession.ActiveSession!=null)return false;
            if(RecommendationDraft389!=null){reason="Terminer ou annuler le projet de construction actuel.";return false;}
            if(ConstructionInput349.ModalOpen||points.Count>0||pendingPositions!=null)
            {reason="Confirmer ou annuler le tracé/devis en cours avant de préparer ce test.";return false;}
            if(controls==null||controls.Count<2){reason="Tracé de test incomplet.";return false;}
            var samples=new List<Vector3>();
            foreach(var p in controls)
            {
                if(!ManagementSurface.TryTrail(p,out var ground)){reason="Tracé hors du terrain actuel.";return false;}
                samples.Add(ground);
            }
            SlopeDesignAssessment design;
            var temporary=new GameObject("ValAlpin — analyse native du devis de piste");
            try
            {
                var tool=temporary.AddComponent<SlopeDesignIntelligenceRuntime>();
                var route=tool.ConformAndResample(samples,sampleSpacing);
                design=tool.Assess(route,SlopeDifficulty.Green);
            }
            finally { Destroy(temporary); }
            if(!design.valid||(int)design.recommendedDifficulty>(int)SlopeDifficulty.Blue)
            {reason="Le relief actuel ne permet pas le retour facile proposé : "+design.message;return false;}
            CancelPendingSlope();SetActive(true);SetWidth(8);
            foreach(var p in samples)AddPoint(p);
            // Use the native recommendation; do not label a blue descent as green.
            SetDifficulty((SlopeConstructionDifficulty)design.recommendedDifficulty);
            Finish();
            bool ready=CanConfirm349(out reason);
            if(!ready&&!string.IsNullOrEmpty(valAlpinRefusal))reason=valAlpinRefusal;
            return ready;
        }
    }
}
