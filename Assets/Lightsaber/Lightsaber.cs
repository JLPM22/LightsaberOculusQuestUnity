﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lightsaber : MonoBehaviour
{
    public Transform Blade;
    public Light Light;
    public float Speed = 1.0f;

    private bool Saved = true;
    private bool Completed = true;

    private IEnumerator ScaleBlade()
    {
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
    }
}
