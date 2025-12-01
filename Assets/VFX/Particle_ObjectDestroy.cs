using System.Collections;
using UnityEngine;


public class Particle_ObjectDestroy : MonoBehaviour
{
    void Start()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null) { Destroy(gameObject); return; }
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        StartCoroutine(WaitForParticlesAndDestroy(ps));
    }

    private IEnumerator WaitForParticlesAndDestroy(ParticleSystem ps)
    {
        yield return new WaitUntil(() => !ps.IsAlive(true));
        Destroy(gameObject);
    }
}
