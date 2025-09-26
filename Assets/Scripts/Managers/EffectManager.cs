using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    [SerializeField] private GameObject craftParticles;
    [SerializeField] private GameObject failParticles;

    private void Awake()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayCraftEffect(Vector3 position, Color color)
    {
        if (craftParticles != null)
        {
            GameObject particles = Instantiate(craftParticles, position, Quaternion.Euler(-90, 0, 0));
            var ps = particles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
            }
        }
    }

    public void PlayFailEffect(Vector3 position)
    {
        if (failParticles != null)
            Instantiate(failParticles, position, Quaternion.Euler(-90, 0, 0));
    }
}