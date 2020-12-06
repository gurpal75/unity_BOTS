using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement me;

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
    public SpriteRenderer sign;

    [Header("Spawner")]

    public GameObject[] enemies;

    [Header("Sounds")]

    public AudioClip[] footsteps;
    public AudioClip[] slash;
    public AudioClip[] slash_hit;
    public AudioClip[] music;
    public AudioClip gruntClip;

    [Header("Components")]

    public Rigidbody2D rb;
    public Animator animator;
    public AudioSource audioSource;
    public AudioSource musicSource;
    public AudioSource fireSource;
    public SpriteRenderer slashFX;
    public CinemachineVirtualCamera cmr;
    public PostProcessVolume volume;
    public Entity entity;
    public GameObject coinPrefab;
    public NPCSpawner spawner;

    [Header("Time")]

    public int dayCount = 10;
    public float timeSpeedUp = 5f;
    public float dayInMinutes = 3f;
    public AnimationCurve dayTimeLight;

    [Header("UI")]

    public TMPro.TMP_Text timeOfDayTxt;
    public TMPro.TMP_Text salaryTxt;
    public TMPro.TMP_Text taxesTxt;
    public TMPro.TMP_Text resumeTxt;
    public RectTransform progressTime;
    public Transform townHall;

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
    float spawnerTime = 0f;

    float musicMomentum = 0f;
    float musicIntensity = 0f;
    float knockbackIntensity = 0f;
    float timeSpeed = 1f;

    float vandalismRate = 0f;
    float timeOfDaySeconds = 0f;

    float reputation = 0f;

    Vector3 spawnLocation;

    CinemachineBasicMultiChannelPerlin noise;
    ChromaticAberration chromatic;
    ColorGrading grading;

    const float defaultTaxes = 0.40f;

    float salary = 1300f;
    float taxes = defaultTaxes;

    float todayEarnings = 0f;
    float totalEarnigs = 0f;

    bool vandalized = false;
    float lastTaxes = defaultTaxes;

    public float GetVandalismRate()
    {
        return vandalismRate;
    }

    public bool IsVandalising()
    {
        return musicMomentum > 0;
    }

    public void SpawnCoin(Vector3 p)
    {
        Instantiate(coinPrefab, p, Quaternion.identity);
    }

    internal void PickedUpCoin()
    {
        todayEarnings += Random.Range(0.1f, 1f);
    }

    int dayIndex = 0;
    int lastDayIndex = 0;

    private void Awake()
    {
        salary = 2333f * (1f - taxes);
        spawnLocation = transform.position;
        me = this;

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

    bool pausedTime = false;
    bool wasalive = true;

    void Update()
    {
        if (!vandalized && IsVandalising())
            vandalized = true;

        if (pausedTime)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                UnpauseTime();

            TimeLogic(true);
            VisualEffectsByMusic(0f);
            return;
        }

        salaryTxt.SetText("Salary: " + salary.ToString("0.00") + "$ ( +" + todayEarnings.ToString("0.00") + "$ )");
        taxesTxt.SetText("Taxes: " + (taxes * 100f).ToString("0.00") + "%");

        // Get the music rithm
        float sample = GetMusicRythm();

        bool alive = entity.Alive();

        if (alive != wasalive)
        {
            wasalive = alive;

            if (!alive)
                NextDayMenu();
        }

        // Handle music logic
        musicSource.volume = musicMomentum;

        if (musicSource.volume != 0 && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        if (musicSource.volume == 0f)
            musicSource.clip = music[Random.Range(0, music.Length)];
        musicMomentum = Mathf.MoveTowards(musicMomentum, 0f, alive ? Time.deltaTime * 0.2f : Time.deltaTime * 5f);

        if (alive && !IsVandalising() && Vector3.Distance(transform.position, townHall.position) < 10f)
        {
            if (Time.time - timerLimitCoin > 2f)
            {
                Instantiate(coinPrefab, transform.position, Quaternion.identity);
                timerLimitCoin = Time.time;

                taxes -= 0.00001f;
            }

            sign.enabled = true;
            sign.transform.localPosition = new Vector3(0, (1f + Mathf.Sin(Time.time * 5f)) * 0.1f, 0);
        }
        else
        {
            sign.enabled = false;
        }

        // Spawn policemen
        SpawnerLogic();

        // SHOOT LASERS THROUGH THE EYES
        if (alive)
            LaserLogic(sample);

        // Use it for screen effects
        VisualEffectsByMusic(sample);

        // Move the player with input, attack on input, etc
        ProcessInput();

        // Camera shake logic
        CameraShake();

        // Day logic
        if (alive)
        {
            timeOfDaySeconds += Time.deltaTime;

            float dayInSeconds = dayInMinutes * 60;
            float hour = ((timeOfDaySeconds % dayInSeconds) / dayInSeconds) * 24f;
            int hour_int = Mathf.FloorToInt(hour);
            int min_int = Mathf.FloorToInt((hour - hour_int) * 60);

            dayIndex = Mathf.FloorToInt(timeOfDaySeconds / dayInSeconds);

            if (dayIndex != lastDayIndex)
            {
                lastDayIndex = dayIndex;
                NextDayMenu();
            }

            timeOfDayTxt.SetText(string.Format("{0}h {1}\n{2} HP",
                hour_int.ToString("00"),
                min_int.ToString("00"), Mathf.FloorToInt((entity.health / 20f) * 100f)));

            float fullTime = dayCount * dayInSeconds;

            progressTime.localScale = new Vector3(timeOfDaySeconds / fullTime, 1, 1);
        }
    }

    void ResetScene()
    {
        transform.position = spawnLocation;
        entity.health = 20f;

        if (vandalized)
        {
            reputation += 0.2f;
            if (reputation > 1f) reputation = 1f;
        }
        else
        {
            reputation -= 0.1f;
            if (reputation < 0) reputation = 0;
        }

        vandalized = false;
    }

    void SetFinishScreen()
    {
        float fullTime = dayCount * dayInMinutes * 60f;

        salaryTxt.SetText("");
        taxesTxt.SetText("");
        timeOfDayTxt.SetText("");

        var resume = string.Format("Current Salary: {0}$\nCurrent Taxes: {1}\nTotal Earned: {2}$\nScore: {3}\n\nPress Space to continue ...",
                salary.ToString("0.00"),
                (taxes * 100f).ToString("0.00"),
                totalEarnigs.ToString("0.00"),
                Mathf.FloorToInt((totalEarnigs * taxes + salary) * 0.1562f) * (timeOfDaySeconds / fullTime));

        resumeTxt.SetText(resume);
    }

    void NextDayMenu()
    {
        pausedTime = true;
        resumeTxt.enabled = true;
        Time.timeScale = 0f;

        float killingRate = (Entity.Destroyed / (float)Entity.Spawned);

        float destructionRate = 0f;
        foreach(var e in Entity.entities)
            destructionRate += e.GetDestructionLevel();
        destructionRate /= Entity.entities.Count;

        Entity.Spawned = 0;
        Entity.Destroyed = 0;

        float destructionPercent = killingRate * 0.3f + destructionRate * 0.7f;
        float taxesIncrease = (todayEarnings * destructionPercent) * 0.01f;

        taxes += taxesIncrease;

        if (taxes < 0.2f) taxes = 0.2f;
        if (taxes > 0.9f) taxes = 0.9f;

        salary = 2333f * (1f - taxes);

        string incdec = (taxes - lastTaxes) > 0 ? "increased" : "decreased";
        string resume = string.Format(
            "<b>Day {0}</b>\n\n"+
            "You earned {1}$ and " + incdec + " the taxes amount by {2}% today.\n" +
            "Destruction level: {3} / 100%\n\n" +
            "Press Space to continue ...",
            dayIndex.ToString(),
            todayEarnings.ToString("0.00"),
            ((taxes - lastTaxes) * 100f).ToString("0.00"),
            (destructionPercent * 100f).ToString("0.00"));

        totalEarnigs += todayEarnings;
        todayEarnings = 0f;

        if (dayIndex == dayCount || !entity.Alive())
             SetFinishScreen();
        else resumeTxt.SetText(resume);

        ResetScene();

        var destructibles = GameObject.FindGameObjectsWithTag("destructible");
        var police = GameObject.FindGameObjectsWithTag("police");

        spawner.Respawn();

        foreach (var p in police)
            Destroy(p);

        foreach (var d in destructibles)
        {
            Destructible2D.D2dDestructible des = d.GetComponent<Destructible2D.D2dDestructible>();
            des.ResetAlpha();
        }
    }

    public void UnpauseTime()
    {
        if (dayIndex == dayCount || !entity.Alive())
        {
            SceneManager.LoadScene(0);
        }

        pausedTime = false;
        resumeTxt.enabled = false;

        lastTaxes = taxes;
    }

    private void TimeLogic(bool moving)
    {
        if (Input.GetKey(KeyCode.Space) && !moving)
        {
            timeSpeed = Mathf.MoveTowards(timeSpeed, timeSpeedUp, Time.unscaledDeltaTime * 5f);
        }
        else
        {
            timeSpeed = Mathf.MoveTowards(timeSpeed, 1f, Time.unscaledDeltaTime * 5f);
        }

        if (timeSpeed != Time.timeScale)
        {
            Time.timeScale = timeSpeed;
            musicSource.pitch = Mathf.Min(timeSpeed, 3f);
            animator.speed = 1f / timeSpeed;
            grading.saturation.value = Mathf.Lerp(0, -100, (timeSpeed - 1f) / (timeSpeedUp - 1));
        }
    }

    private void SpawnerLogic()
    {
        if (musicMomentum > 0)
        {
            if (Time.time - spawnerTime > 0.5f && NPCBrain.NPCCount < Mathf.Lerp(10, 80, reputation))
            {
                int count = Mathf.FloorToInt(musicMomentum * 0.6f);

                for (int i = 0; i < count; ++i)
                {
                    var pos = Random.insideUnitCircle.normalized * Random.Range(20f, 25f);
                    Instantiate(enemies[Random.Range(0, enemies.Length)], pos, Quaternion.identity);
                }

                spawnerTime = Time.time;
            }
        }
    }

    private void LaserLogic(float sample)
    {
        if (musicSource.isPlaying && !musicPaused && musicMomentum == 0)
        {
            musicSource.Pause();
            musicPaused = true;
        }

        if (musicMomentum > 5f && (sample / musicSource.volume) > 0.7f)
            laserIntensity = Mathf.Lerp(laserIntensity, 1f, Time.deltaTime * 50f);
        else laserIntensity = Mathf.MoveTowards(laserIntensity, 0f, Time.deltaTime);

        fireSource.volume = laserIntensity;

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
                if (lhit.collider != null)
                {
                    AttackCollider(lhit.collider, 10f);
                    Instantiate(redExplosion, lhit.point, Quaternion.identity);
                }
                if (rhit.collider != null)
                {
                    AttackCollider(rhit.collider, 10f);
                    Instantiate(redExplosion, rhit.point, Quaternion.identity);
                }
            }
        }
        else if (shootingLaser)
        {
            shootingLaser = false;
            laserLeft.enabled = false;
            laserRight.enabled = false;
            knockbackIntensity = 0f;
        }
    }

    private float red = 0f;

    private void VisualEffectsByMusic(float sample)
    {
        if (chromatic != null)
        {
            chromatic.intensity.value = sample;

            red = Mathf.Lerp(0, 1f, (sample + (musicMomentum / 10f)) * 0.5f);
        }
        
        var currentColor = grading.colorFilter.value;
        float dayInSeconds = dayInMinutes * 60;
        var targetColor = Color.Lerp(Color.white, Color.red, red) * dayTimeLight.Evaluate((timeOfDaySeconds % dayInSeconds) / dayInSeconds) * (entity.Alive() ? 1f : 0f);
        grading.colorFilter.value = Color.Lerp(currentColor, targetColor, Time.deltaTime * 10f);
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

        TimeLogic(delta != Vector2.zero);

        bool alive = entity.Alive();

        animator.SetBool("Dead", !alive);
        if (!alive)
        {
            delta = Vector2.zero;
            return;
        }

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
                noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, Mathf.Min(8f, musicMomentum * 5f), Time.deltaTime * 10f);
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, musicIntensity * musicMomentum * 0.15f, Time.deltaTime * 10f);
            }
        }
    }

    public void ShakeCamera(float time)
    {
        shakeTimer = time;
    }

    public void Grunt()
    {
        audioSource.PlayOneShot(gruntClip, 1f);
    }

    public void AttackCollider(Collider2D c, float dmg)
    {
        if (c == null) return;

        var entity = c.GetComponent<Entity>();

        if (entity == null)
        {
            entity = c.GetComponentInParent<Entity>();
            if (entity == null) return;
        }

        if (!entity.immortal)
        {
            Instantiate(coinPrefab, c.bounds.center, Quaternion.identity);
            OnHitSomething();
        }

        Vector2 direction = (entity.transform.position - transform.position).normalized;

        // Make sure we are facing it before we attack it
        // if (Vector2.Dot(direction, attackDir.normalized) > -0.3f)
        {
            var collisionPoint = c.bounds.ClosestPoint(transform.position);
            Instantiate(redExplosion, collisionPoint, Quaternion.identity);

            entity.TakeDamage(dmg, direction * 50f);
        }
    }

    float timerLimitCoin = 0;

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
                entity.TakeDamage(playerDmg + (musicMomentum / 10f) * 3f, direction * 50f);

                if (!entity.immortal && Time.time - timerLimitCoin > 0.2f)
                {
                    Instantiate(coinPrefab, collisionPoint, Quaternion.identity);
                    timerLimitCoin = Time.time;
                }
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
        Vector2 knockBack = -mouseDir.normalized * knockbackIntensity * Time.fixedUnscaledDeltaTime;

        // If moving, move the actual position
        if (moving || knockBack != Vector2.zero)
        {
            rb.MovePosition(rb.position + delta * moveSpeed * Time.fixedUnscaledDeltaTime + knockBack);

            // Handle the footstep sounds here
            footstep_timer += Time.fixedUnscaledDeltaTime;
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
