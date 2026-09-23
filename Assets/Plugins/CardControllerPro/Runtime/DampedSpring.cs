using System;
using UnityEngine;

namespace CCP
{

[Serializable]
public class DampedSpring {
    [Tooltip("How fast the spring returns to center. Lower = lazier.")]
    public float stiffness = 150f;

    [Tooltip("Oscillation amount. 10 = bouncy wobble, 25 = smooth return.")]
    public float damping = 10f;

    [NonSerialized] public float displacement;
    [NonSerialized] public float velocity;

    public void Update(float dt) {
        float force = -stiffness * displacement - damping * velocity;
        velocity += force * dt;
        displacement += velocity * dt;
    }

    public void AddVelocity(float impulse) {
        velocity += impulse;
    }

    public void Reset() {
        displacement = 0f;
        velocity = 0f;
    }

    public bool IsAtRest(float displacementThreshold = 0.01f, float velocityThreshold = 0.01f) {
        return Mathf.Abs(displacement) < displacementThreshold
            && Mathf.Abs(velocity) < velocityThreshold;
    }
}

}
