using UnityEngine;

namespace CCP
{
    public interface IPointable
    {
        void PointAt(Vector2 beginPosition, Vector2 endPosition);
        void Hide();
    }
}
