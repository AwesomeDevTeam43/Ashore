using System.IO;
using System.Text;
using UnityEngine;

namespace Ashore.AI.DecisionTree
{
    // Logs features + chosen action to a CSV for offline training.
    // Attach this to an enemy GameObject alongside a FeatureProvider.
    public class TelemetryLogger : MonoBehaviour
    {
        [Header("Logging")]
        [SerializeField] private string fileName = "telemetry.csv"; // written under Application.persistentDataPath by default
        [SerializeField] private bool writeHeaders = true;
        [SerializeField] private float logInterval = 0.25f; // seconds between logs
        [SerializeField] private bool usePersistentDataPath = true; // if false, uses project-relative path

        [Header("Sources")]
        [SerializeField] private MonoBehaviour featureProviderBehaviour; // must implement IFeatureProvider
        [SerializeField] private EnemyBase enemyBase; // to read current AI state

        private IFeatureProvider featureProvider;
        private float timer;
        private string fullPath;
        private bool headersWritten;

        private void Awake()
        {
            featureProvider = featureProviderBehaviour as IFeatureProvider;
            if (enemyBase == null) enemyBase = GetComponent<EnemyBase>();

            string path = fileName;
            if (usePersistentDataPath)
            {
                path = Path.Combine(Application.persistentDataPath, fileName);
            }
            fullPath = path;
            timer = 0f;
        }

        private void Update()
        {
            if (featureProvider == null || enemyBase == null) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = logInterval;
                LogSample();
            }
        }

        private void LogSample()
        {
            int n = featureProvider.FeatureCount;
            var feats = new float[n];
            featureProvider.CollectFeatures(feats);

            // Action id - try to get from IAIStateProvider if implemented, otherwise default to 0
            int actionId = 0;
            if (enemyBase is IAIStateProvider stateProvider)
            {
                actionId = stateProvider.CurrentAIState switch
                {
                    Utilities.SimpleEnemyState.Roaming => 0,
                    Utilities.SimpleEnemyState.AttackWindup => 1,
                    Utilities.SimpleEnemyState.Lunging => 2,
                    Utilities.SimpleEnemyState.Retreating => 3,
                    _ => 0
                };
            }

            // Build CSV line
            var sb = new StringBuilder();
            for (int i = 0; i < feats.Length; i++)
            {
                sb.Append(feats[i].ToString("G"));
                sb.Append(i < feats.Length - 1 ? "," : ",");
            }
            sb.Append(actionId);

            // Ensure directory
            try
            {
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Write headers once if requested and file doesn't exist
                if (writeHeaders && !headersWritten && !File.Exists(fullPath))
                {
                    var header = new StringBuilder();
                    // Default names when metadata is unavailable
                    header.Append("dist_to_player,vertical_delta,has_los,player_health_ratio,enemy_health_ratio,cooldown,action_id\n");
                    File.WriteAllText(fullPath, header.ToString());
                    headersWritten = true;
                }

                File.AppendAllText(fullPath, sb.ToString() + "\n");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"TelemetryLogger write failed: {ex.Message}");
            }
        }
    }
}