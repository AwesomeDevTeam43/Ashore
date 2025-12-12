using System;
using System.IO;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

namespace Ashore.AI.DecisionTree
{
    // Barracuda ONNX decision policy that outputs an action id.
    public class OnnxDecisionPolicy : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private NNModel onnxAsset;
        [SerializeField] private TextAsset metadataJson; // contains feature_names and action_mapping
        [SerializeField] private string inputName = "input"; // matches skl2onnx name
        [SerializeField] private string outputName = "label"; // skl2onnx usually uses 'label' or 'output_label'

        [Header("Features")]
        [SerializeField] private MonoBehaviour featureProviderBehaviour; // must implement IFeatureProvider

        private IFeatureProvider featureProvider;
        private Model _model;
        private IWorker _worker;
        private string[] _featureNames;

        public enum ActionId { Roaming = 0, AttackWindup = 1, Lunging = 2, Retreating = 3 }

        private void Awake()
        {
            featureProvider = featureProviderBehaviour as IFeatureProvider;
            if (onnxAsset != null)
            {
                _model = ModelLoader.Load(onnxAsset);
                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _model);
            }
            ParseMetadata();
        }

        private void OnDestroy()
        {
            _worker?.Dispose();
        }

        private void ParseMetadata()
        {
            if (metadataJson == null) return;
            try
            {
                var obj = JsonUtility.FromJson<MetaWrapper>(metadataJson.text);
                _featureNames = obj.feature_names;
            }
            catch (Exception)
            {
                // ignore, optional
            }
        }

        [Serializable]
        private class MetaWrapper
        {
            public string[] feature_names;
        }

        public bool TryDecide(out ActionId action)
        {
            action = ActionId.Roaming;
            if (_worker == null || featureProvider == null) return false;

            int n = featureProvider.FeatureCount;
            var feats = new float[n];
            featureProvider.CollectFeatures(feats);

            // Prepare tensor [1, n]
            using var input = new Tensor(1, n, feats);
            _worker.Execute(new Dictionary<string, Tensor> { { string.IsNullOrEmpty(inputName) ? _model.inputs[0].name : inputName, input } });
            using var output = _worker.PeekOutput(string.IsNullOrEmpty(outputName) ? _model.outputs[0] : outputName);

            // For sklearn classifiers, output may be int class or probability vector depending on converter settings.
            // Handle common case: single value class id.
            int cls = 0;
            if (output.length >= 1)
            {
                // Barracuda outputs floats; round to nearest int id
                cls = Mathf.RoundToInt(output[0]);
            }
            action = (ActionId)Mathf.Clamp(cls, 0, 3);
            return true;
        }
    }
}
