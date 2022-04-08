using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class EvaluateController : PlaneController
    {
        public TextMeshPro evaluationText;

        private static int episodeCount=0;
        private static int finishCount = 0;
        private static int failedCount = 0;
        private static float finishUseTime = 0f;

        public new void Start()
        {
            base.Start();
            RefreshEvaluationText();
        }

        private void RefreshEvaluationText()
        {
            if(evaluationText==null)
                return;
            evaluationText.text = $"episode count: {episodeCount}\n" +
                                  $"finish: {finishCount}\n" +
                                  $"failed: {failedCount}\n" +
                                  $"total: {finishCount + failedCount}\n" +
                                  $"finish rate: {(float) finishCount / (finishCount + failedCount)}\n" +
                                  $"average use: {finishUseTime / finishCount}";
        }

        protected override void ResetPlane()
        {
            base.ResetPlane();
            episodeCount++;
            RefreshEvaluationText();
        }

        protected override void OrderFinished(Order o, bool isNewOrder = false)
        {
            base.OrderFinished(o, isNewOrder);
            finishCount++;
            finishUseTime += Time.fixedTime - o.GenerateTime;
        }

        protected override void OrderFailed(Order o)
        {
            base.OrderFailed(o);
            failedCount++;
        }
    }
}