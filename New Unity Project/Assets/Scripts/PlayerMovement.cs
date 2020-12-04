using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    [Header("Parameters")]

    public float moveSpeed = 5f;
    public float footstepsSoundRate = 0.2f;
    public float slashAngleOffset = 0f;
    public float attackRate = 0.1f;
    public float attackRange = 2f;
    public float attackOffsetY = 0f;
    public LayerMask attackLayer;

    public float playerDmg = 1f;

    public GameObject redExplosion;

    [Header("Lasers")]

    public SpriteRenderer laserLeft;
    public SpriteRenderer laserRight;

    [Header("Sounds")]

    public AudioClip[] footsteps;
    public AudioClip[] slash;
    public AudioClip[] slash_hit;
    public AudioClip[] music;

    [Header("Components")]

    public Rigidbody2D rb;
    public Animator animator;
    public AudioSource audioSource;
    public AudioSource musicSource;
    public SpriteRenderer slashFX;
    public CinemachineVirtualCamera cmr;
    public PostProcessVolume volume;

    Vector2 delta;
    Vector2 lastDirection;
    Vector2 attackDir;
    float footstep_timer = 0f;
    float attack_timer = 0f;
    float shakeTimer = 0f;
    float[] samples = new float[64];
    bool musicPaused = false;
    float laserDestroyTimer = 0f;
    bool shootingLaser = false;
    float laserIntensity = 0f;

    float musicMomentum = 0f;
    float musicIntensity = 0f;
    float knockbackIntensity = 0f;

    CinemachineBasicMultiChannelPerlin noise;
    ChromaticAberration chromatic;
    ColorGrading grading;

    private void Awake()
    {
        if (cmr != null)
        {
            noise = cmr.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            noise.m_AmplitudeGain = 0f;
        }

        if (volume != null)
        {
            chromatic = volume.profile.GetSetting<ChromaticAberration>();
            grading = volume.profile.GetSetting<ColorGrading>();
        }

        musicSource.clip = music[Random.Range(0, music.Length)];
    }

    void Update()
    {
        // Get the music rithm
        float sample = GetMusicRythm();

        // Handle music logic
        musicSource.volume = musicMomentum;
        musicMomentum = Mathf.MoveTowards(musicMomentum, 0f, Time.deltaTime * 0.2f);

        if (musicSource.isPlaying && !musicPaused && musicMomentum == 0)
        {
            musicSource.Pause();
            musicPaused = true;
        }

        if (musicMomentum > 5f && (sample / musicSource.volume) > 0.6f)
             laserIntensity = Mathf.Lerp(laserIntensity, 1f, Time.deltaTime * 50f);
        else laserIntensity = Mathf.MoveTowards(laserIntensity, 0f, Time.deltaTime);

        if (laserIntensity > 0.001f)
        {
            if (!shootingLaser)
            {
                shootingLaser = true;
                laserLeft.enabled = true;
                laserRight.enabled = true;
            }

            knockbackIntensity = 2f;

            var mouseDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            float angleForward = Vector2.SignedAngle(Vector2.up, mouseDir) - 90f;
            laserLeft.transform.eulerAngles = new Vector3(0, 0, angleForward);
            laserRight.transform.eulerAngles = new Vector3(0, 0, angleForward);

            RaycastHit2D lhit = Physics2D.Raycast(laserLeft.transform.position, mouseDir, 500f, attackLayer);
            RaycastHit2D rhit = Physics2D.Raycast(laserRight.transform.position, mouseDir, 500f, attackLayer);

            float width = Mathf.Lerp(0f, 0.21f, laserIntensity);

            laserLeft.transform.localScale = new Vector3(lhit.collider == null ? 500f : lhit.distance, width, 1f);
            laserRight.transform.localScale = new Vector3(rhit.collider == null ? 500f : rhit.distance, width, 1f);

            if (Time.time - (laserDestroyTimer + (1f - laserIntensity)) > 0.1f)
            {
                laserDestroyTimer = Time.time;
                if (lhit.collider != null) Instantiate(redExplosion, lhit.point, Quaternion.identity);
                if (rhit.collider != null) Instantiate(redExplosion, rhit.point, Quaternion.identity);
            }
        }
        else if (shootingLaser)
        {
            shootingLaser = false;
            laserLeft.enabled = false;
            laserRight.enabled = false;
            knockbackIntensity = 0f;
        }

        // Use it for screen effects
        VisualEffectsByMusic(sample);

        // Move the player with input, attack on input, etc
        ProcessInput();

        // Camera shake logic
        CameraShake();
    }

    private void VisualEffectsByMusic(float sample)
    {
        if (chromatic != null)
        {
            chromatic.intensity.value = sample;

            var currentColor = grading.colorFilter.value;
            var targetColor = Color.Lerp(Color.white, Color.red, (sample + (musicMomentum / 10f)) * 0.5f);
            grading.colorFilter.value = Color.Lerp(currentColor, targetColor, Time.deltaTime * 10f);
        }
    }

    private float GetMusicRythm()
    {
        musicSource.GetSpectrumData(this.samples, 0, FFTWindow.BlackmanHarris);

        float sample = samples[0];

        if (sample > musicIntensity) musicIntensity = sample;

        musicIntensity = Mathf.MoveTowards(musicIntensity, sample, Time.deltaTime);

        return sample;
    }

    private void ProcessInput()
    {
        // Get user input to move
        delta.x = -Input.GetAxisRaw("Horizontal");
        delta.y = Input.GetAxisRaw("Vertical");

        // Keep track of the facing direction
        if (delta != Vector2.zero)
            lastDirection = delta;

        // Attack logic
        if (Input.GetMouseButtonDown(0) && Time.time - attack_timer > attackRate)
        {
            animator.SetTrigger("Attack");
            attack_timer = Time.time;
            attackDir = lastDirection;
        }
    }

    private void CameraShake()
    {
        if (noise != null)
        {
            if (shakeTimer > 0)
            {
                noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, Mathf.Min(2f, musicMomentum * 5f), Time.deltaTime * 10f);
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, musicIntensity * musicMomentum * 0.15f, Time.deltaTime * 10f);
            }
        }
    }

    void ShakeCamera(float time)
    {
        shakeTimer = time;
    }

    // This method is called by the animation
    void Attack()
    {
        // Make the slash face the player direction
        var angle = Vector2.SignedAngle(Vector2.up, attackDir.normalized);
        slashFX.transform.eulerAngles = new Vector3(0, 0, angle + slashAngleOffset);

        // Get all the entities around me
        var collisions = Physics2D.OverlapCircleAll(transform.position + Vector3.up * attackOffsetY, attackRange, attackLayer);

        // Keep track if we hit something
        bool hitSomething = false;

        for (int i = 0; i < collisions.Length; ++i)
        {
            var entity = collisions[i].GetComponent<Entity>();

            if (entity == null)
            {
                entity = collisions[i].GetComponentInParent<Entity>();
                if (entity == null) continue;
            }

            Vector2 direction = (entity.transform.position - transform.position).normalized;

            // Make sure we are facing it before we attack it
            // if (Vector2.Dot(direction, attackDir.normalized) > -0.3f)
            {
                var collisionPoint = collisions[i].bounds.ClosestPoint(transform.position);
                Instantiate(redExplosion, collisionPoint, Quaternion.identity);

                entity.TakeDamage(playerDmg, direction * 50f);
                hitSomething = true;
            }
        }

        // Play the slash sound

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = hitSomething ? 
            slash_hit[Random.Range(0, slash_hit.Length)] :
            slash[Random.Range(0, slash.Length)];
        audioSource.Play();

        if (hitSomething)
        {
            OnHitSomething();
        }
    }

    void OnHitSomething()
    {
        ShakeCamera(0.1f);

        // Resume music
        musicMomentum = 0.5f + musicMomentum;
        if (musicMomentum > 10f)
            musicMomentum = 10f;

        if (musicPaused)
        {
            musicSource.UnPause();
        }
        else if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    void FixedUpdate()
    {
        bool moving = delta != Vector2.zero;

        // Handle animations
        animator.SetBool("Moving", moving);
        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);

        Vector2 mouseDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Vector2 knockBack = -mouseDir * knockbackIntensity * Time.fixedDeltaTime;

        // If moving, move the actual position
        if (moving || knockBack != Vector2.zero)
        {
            rb.MovePosition(rb.position + delta * moveSpeed * Time.fixedDeltaTime + knockBack.normalized);

            // Handle the footstep sounds here
            footstep_timer += Time.fixedDeltaTime;
            if (footstep_timer > footstepsSoundRate)
            {
                audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
                footstep_timer = 0f;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position + Vector3.up * attackOffsetY, attackRange);
    }
}
