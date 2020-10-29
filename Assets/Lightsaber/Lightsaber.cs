using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lightsaber : MonoBehaviour
{
    public Transform Blade;
    public Light Light;
    public float Speed = 1.0f;
    [Header("Sounds")]
    public AudioClip Idle;
    public AudioClip PowerUp;
    public AudioClip PowerDown;

    private bool Saved = true;
    private bool Completed = true;
    private float LightIntensity;

    private AudioSource AudioSource;

    private void Awake()
    {
        LightIntensity = Light.intensity;
        AudioSource = GetComponent<AudioSource>();
        AudioSource.clip = Idle;
        AudioSource.loop = true;
    }

    private IEnumerator ScaleBlade()
    {
        if (Saved)
        {
            Blade.gameObject.SetActive(true);
            AudioSource.PlayOneShot(PowerUp);
        }
        else
        {
            AudioSource.Stop();
            AudioSource.PlayOneShot(PowerDown);
        }

        Completed = false;
        while ((Blade.localScale.x != 1.0f && Saved) || (Blade.localScale.x != 0.0f && !Saved))
        {
            float nextScale = Blade.localScale.x;

            if (Saved) nextScale += Time.deltaTime * Speed;
            else nextScale -= Time.deltaTime * Speed;

            Vector3 currentScale = Blade.localScale;
            currentScale.x = Mathf.Clamp(nextScale, 0.0f, 1.0f);
            Blade.localScale = currentScale;

            yield return null;
        }

        if (!Saved) Blade.gameObject.SetActive(false);
        else AudioSource.Play();

        Completed = true;
        Saved = !Saved;
    }

    private void Update()
    {
        /*
         OVRInput.Button.One = A
         OVRInput.Button.Two = B
         OVRInput.Button.Three = X
         OVRInput.Button.Four = Y
        */
        if (Completed && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            Light.enabled = Saved;
            StartCoroutine(ScaleBlade());
        }

        // Light Blinking
        Light.intensity = LightIntensity + 0.1f * Mathf.Abs(Mathf.Sin(20.0f * Time.time));
    }
}
