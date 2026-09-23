using System.Collections.Generic;
using UnityEngine;

namespace CCP
{
    public class DynamicArrow : MonoBehaviour, IPointable
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject headPrefab;

        [Header("Node Settings")]
        [SerializeField] private float distancePerNode = 50f;
        [SerializeField] private int maxNodes = 20;

        [Header("Scale Settings")]
        [SerializeField] private float firstNodeScale = 0.5f;
        [SerializeField] private float lastNodeScale = 1f;
        [SerializeField] private float headScale = 1f;

        private readonly List<RectTransform> _nodePool = new List<RectTransform>();
        private RectTransform _headInstance;
        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private RectTransform _canvasRect;

        private Vector2 _cachedBegin;
        private Vector2 _cachedEnd;
        private int _activeNodeCount;
        private bool _isVisible;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            CreateHeadInstance();
        }

        private void CreateHeadInstance()
        {
            if (headPrefab == null || _headInstance != null) return;

            var headObj = Instantiate(headPrefab, transform);
            _headInstance = headObj.GetComponent<RectTransform>();
            if (_headInstance == null)
            {
                _headInstance = headObj.AddComponent<RectTransform>();
            }
            _headInstance.gameObject.SetActive(false);
        }

        public void PointAt(Vector2 canvasBeginPosition, Vector2 canvasEndPosition)
        {
            // Cache canvas components on first use
            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>();
                if (_parentCanvas == null)
                {
                    Debug.LogError("DynamicArrow must be a child of a Canvas to use PointAt()");
                    return;
                }
                _canvasRect = _parentCanvas.transform as RectTransform;
                if (_canvasRect == null)
                {
                    Debug.LogError("Canvas must have a RectTransform component");
                    return;
                }
            }

            // Convert canvas coordinates to arrow-local coordinates
            Vector3 localBegin3D = _rectTransform.InverseTransformPoint(
                _canvasRect.TransformPoint(canvasBeginPosition));
            Vector2 beginPosition = new Vector2(localBegin3D.x, localBegin3D.y);

            Vector3 localEnd3D = _rectTransform.InverseTransformPoint(
                _canvasRect.TransformPoint(canvasEndPosition));
            Vector2 endPosition = new Vector2(localEnd3D.x, localEnd3D.y);

            // Rest of the method unchanged (check cache, calculate direction, etc.)
            if (beginPosition == _cachedBegin && endPosition == _cachedEnd && _isVisible)
            {
                return;
            }

            _cachedBegin = beginPosition;
            _cachedEnd = endPosition;
            _isVisible = true;

            Vector2 direction = endPosition - beginPosition;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                HideAllNodes();
                return;
            }

            Vector2 normalizedDirection = direction / distance;
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

            int requiredNodes = Mathf.Clamp(Mathf.FloorToInt(distance / distancePerNode), 1, maxNodes);

            EnsureNodePoolSize(requiredNodes);
            UpdateNodes(beginPosition, normalizedDirection, distance, requiredNodes, angle);
            UpdateHead(endPosition, angle);
            DeactivateExcessNodes(requiredNodes);

            _activeNodeCount = requiredNodes;
        }

        public void Hide()
        {
            if (!_isVisible) return;

            _isVisible = false;
            HideAllNodes();

            if (_headInstance != null)
            {
                _headInstance.gameObject.SetActive(false);
            }

            _cachedBegin = Vector2.zero;
            _cachedEnd = Vector2.zero;
        }

        private void EnsureNodePoolSize(int requiredCount)
        {
            while (_nodePool.Count < requiredCount)
            {
                if (nodePrefab == null) break;

                var nodeObj = Instantiate(nodePrefab, transform);
                var nodeRect = nodeObj.GetComponent<RectTransform>();
                if (nodeRect == null)
                {
                    nodeRect = nodeObj.AddComponent<RectTransform>();
                }
                nodeObj.SetActive(false);
                _nodePool.Add(nodeRect);
            }
        }

        private void UpdateNodes(Vector2 begin, Vector2 direction, float distance, int nodeCount, float angle)
        {
            float nodeSpacing = distance / (nodeCount + 1);

            for (int i = 0; i < nodeCount; i++)
            {
                var node = _nodePool[i];
                node.gameObject.SetActive(true);

                float t = (float)(i + 1) / (nodeCount + 1);
                Vector2 position = begin + direction * (nodeSpacing * (i + 1));

                node.anchoredPosition = position;
                node.localRotation = Quaternion.Euler(0f, 0f, angle);

                float scale = Mathf.Lerp(firstNodeScale, lastNodeScale, t);
                node.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void UpdateHead(Vector2 position, float angle)
        {
            if (_headInstance == null) return;

            _headInstance.gameObject.SetActive(true);
            _headInstance.anchoredPosition = position;
            _headInstance.localRotation = Quaternion.Euler(0f, 0f, angle);
            _headInstance.localScale = new Vector3(headScale, headScale, 1f);
        }

        private void DeactivateExcessNodes(int activeCount)
        {
            for (int i = activeCount; i < _activeNodeCount; i++)
            {
                if (i < _nodePool.Count)
                {
                    _nodePool[i].gameObject.SetActive(false);
                }
            }
        }

        private void HideAllNodes()
        {
            for (int i = 0; i < _activeNodeCount; i++)
            {
                if (i < _nodePool.Count)
                {
                    _nodePool[i].gameObject.SetActive(false);
                }
            }
            _activeNodeCount = 0;
        }

        private void OnDestroy()
        {
            foreach (var node in _nodePool)
            {
                if (node != null)
                {
                    Destroy(node.gameObject);
                }
            }
            _nodePool.Clear();

            if (_headInstance != null)
            {
                Destroy(_headInstance.gameObject);
            }
        }
    }
}
