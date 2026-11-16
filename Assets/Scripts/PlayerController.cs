using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

// PlayerController는 플레이어 캐릭터로서 Player 게임 오브젝트를 제어한다.
public class PlayerController : MonoBehaviour {
    [Serializable]
    public class CharacterSoundSet
    {
        public RuntimeAnimatorController controller;
        public AudioClip jumpClip;
        public AudioClip deathClip;
    }

    public AudioClip deathClip; // 사망시 재생할 오디오 클립
    public float jumpForce = 700f; // 점프 힘
    [SerializeField] private CharacterSoundSet[] characterSounds = Array.Empty<CharacterSoundSet>();

    private int jumpCount = 0; // 누적 점프 횟수
    private bool isDead = false; // 사망 상태

    private Rigidbody2D playerRigidbody; // 사용할 리지드바디 컴포넌트
    private Animator animator; // 사용할 애니메이터 컴포넌트
    private AudioSource playerAudio; // 사용할 오디오 소스 컴포넌트
    private PlayerEffects effects;
    private Collider2D bodyCollider;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private ScrollingObject groundScroll;
    private AudioClip defaultJumpClip;
    private AudioClip currentJumpClip;
    private AudioClip currentDeathClip;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();
        defaultJumpClip = playerAudio != null ? playerAudio.clip : null;
        ApplyCharacterSounds();
        effects = GetComponent<PlayerEffects>();
        bodyCollider = GetComponent<Collider2D>();

        playerRigidbody.simulated = false;
        animator.speed = 1f;
        animator.SetBool("Grounded", true);
    }

    public void PreviewCharacter(RuntimeAnimatorController controller)
    {
        if (controller == null || isDead) return;
        animator.runtimeAnimatorController = controller;
        ApplyCharacterSounds();
        animator.Rebind();
        animator.SetBool("Grounded", true);
        animator.Play("Run", 0, 0f);
        animator.Update(0f);
    }

    private void ApplyCharacterSounds()
    {
        currentJumpClip = defaultJumpClip;
        currentDeathClip = deathClip;
        foreach (var sounds in characterSounds)
        {
            if (sounds == null || sounds.controller != animator.runtimeAnimatorController) continue;
            if (sounds.jumpClip != null) currentJumpClip = sounds.jumpClip;
            if (sounds.deathClip != null) currentDeathClip = sounds.deathClip;
            break;
        }
    }

    private void PlayVoice(AudioClip clip)
    {
        if (playerAudio == null) return;
        // Restart jump voices cleanly; a death voice replaces any unfinished jump.
        playerAudio.Stop();
        playerAudio.clip = clip;
        if (clip != null) playerAudio.Play();
    }

    public void BeginRun()
    {
        playerRigidbody.simulated = true;
        animator.SetBool("Grounded", false);
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (isDead || GameManager.instance == null ||
            !GameManager.instance.CanAcceptGameplayInput) return;
        if (ctx.performed)
        {
            TryJump();
        }
        else if(ctx.canceled && playerRigidbody.linearVelocity.y > 0)
        {
            playerRigidbody.linearVelocity = new Vector2 ( playerRigidbody.linearVelocity.x, playerRigidbody.linearVelocity.y * 0.5f);
        }
    }
    void TryJump()
    {
        if (jumpCount >= 2) return;
        // Surface dust only appears when taking off from the ground.
        if (groundContacts.Count > 0)
        {
            var feet = bodyCollider != null
                ? new Vector3(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y, transform.position.z)
                : transform.position;
            effects?.PlayJump(feet, groundScroll);
        }
        groundContacts.Clear();
        groundScroll = null;
        jumpCount++;
        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.AddForce(new Vector2(0, jumpForce));
        PlayVoice(currentJumpClip);
        animator?.SetBool("Grounded", false);
        // Restart the motion on every successful jump, including the second jump.
        animator?.Play("Base Layer.Jump", 0, 0f);
    }
   private void Die() {
        // 사망 처리
        animator.SetTrigger("Die");
        PlayVoice(currentDeathClip);

        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.simulated = false;
        isDead = true;

        GameManager.instance.OnPlayerDead();
   }

   private void OnTriggerEnter2D(Collider2D other) {
       // 트리거 콜라이더를 가진 장애물과의 충돌을 감지
       if (other.CompareTag("Dead") && !isDead &&
           GameManager.instance != null && GameManager.instance.IsPlaying)
        {
            Die();
        }
   }

   private void OnCollisionEnter2D(Collision2D collision) {
        UpdateGroundContact(collision);
   }

   private void OnCollisionStay2D(Collision2D collision) {
        UpdateGroundContact(collision);
   }

   private void UpdateGroundContact(Collision2D collision) {
        if (isDead || GameManager.instance == null || !GameManager.instance.IsPlaying) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.normal.y <= 0.7f || playerRigidbody.linearVelocity.y > 0.1f) continue;

            bool justLanded = groundContacts.Count == 0;
            groundContacts.Add(collision.collider);
            groundScroll = collision.collider.GetComponentInParent<ScrollingObject>();
            jumpCount = 0;
            animator?.SetBool("Grounded", true);
            if (justLanded)
            {
                float x = bodyCollider != null ? bodyCollider.bounds.center.x : transform.position.x;
                effects?.PlayLanding(new Vector3(x, contact.point.y, transform.position.z), groundScroll);
            }
            return;
        }

        RemoveGroundContact(collision.collider);
   }

   private void OnCollisionExit2D(Collision2D collision) {
        RemoveGroundContact(collision.collider);
   }

   private void RemoveGroundContact(Collider2D ground) {
        groundContacts.Remove(ground);
        if (groundContacts.Count == 0)
        {
            groundScroll = null;
            animator?.SetBool("Grounded", false);
        }
   }
}
