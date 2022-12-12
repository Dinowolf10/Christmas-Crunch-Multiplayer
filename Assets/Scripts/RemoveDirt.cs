using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// file: RemoveDirt.cs
/// description: Handles the removal of Dirt Particles from the scene
/// author: Nathan Ballay
/// </summary>
public class RemoveDirt : MonoBehaviour
{
    // References
    private GameManager gameManager;
    private SoundManager soundManager;
    private NetworkRunner runner;

    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        soundManager = GameObject.Find("SoundManager").GetComponent<SoundManager>();

        runner = gameManager.GetRunner();
    }

    /// <summary>
    /// Removes this dirt particle when the mouse passes over
    /// </summary>
    private void OnMouseEnter()
    {
        if (runner)
        {
            return;
        }

        if (!gameManager.isGamePaused())
        {
            soundManager.PlayVacuumSuckSound();
            Destroy(this.gameObject);
        }
    }
}
