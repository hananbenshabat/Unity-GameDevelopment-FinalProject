using System.Collections;
using UnityEngine;

public class DoorRotationLite : MonoBehaviour
{
    private Animator anim;
    private AudioSource sound;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        sound = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        anim.SetBool("opening", true);
        sound.PlayDelayed(0.8f);
    }

    private void OnTriggerExit(Collider other)
    {
        anim.SetBool("opening", false);
        sound.PlayDelayed(0.2f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
