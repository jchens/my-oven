using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        // audio
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        // worlds
        public bool isSpaceWorld = false;
        public bool goodguys = false;
        public bool timeWorld = false;
        public bool timeWorld_2 = false;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 5; // made not public
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 10; // made not public

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        public Text timeText;
        public Text hintText;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        bool invert = false;
        float multiply = (float)1.0;
        int time = 10;

        void Awake()
        {
            updateHint();
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == "space-1" || currentScene.name == "space-2" || currentScene.name == "space-3") {
                spriteRenderer.flipY = true;
                Physics2D.gravity = new Vector2(0f, 9.81f);
            }
            else {
                spriteRenderer.flipY = false;
                Physics2D.gravity = new Vector2(0f, -9.81f);
            }

            if (currentScene.name == "space-4") {
                timeText.text = "3";
                time = 3;
                InvokeRepeating("FlipGravity", 3f, 3f);
                InvokeRepeating("SpaceTimer", 0f, 1f);
            }

            if(timeWorld) {
                timeText.text = "10";
                InvokeRepeating("Invert", 10f, 10f);
                InvokeRepeating("Timer", 0f, 1f);
            } else if(timeWorld_2) {
                InvokeRepeating("Speed", 10f, 10f);
                InvokeRepeating("Timer", 0f, 1f);
            }

            // InvokeRepeating("hideHint", 10f, 1000f);
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if(invert) {
                    move.x = -1 * move.x;
                }

                if(timeWorld_2) {
                    move.x = multiply * move.x;
                }

                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                 if (isSpaceWorld) {
                    velocity.y *= -1;
                }
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                     if (isSpaceWorld) {
                        velocity.y *= -1;
                    }
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        void FlipGravity() {
            float dir = Physics2D.gravity.y;
            Physics2D.gravity = new Vector2(0f, -dir);
            isSpaceWorld = !isSpaceWorld;
            spriteRenderer.flipY = !spriteRenderer.flipY;
        }

        void Invert()
        {
            invert = !invert;
            Debug.Log("hello");
        }

        void Speed()
        {
            multiply = multiply + (float)0.5;
        }

        void Timer()
        {
            timeText.text = time.ToString();
            time = time - 1;
            if(time == 0) {
                time = 10;
            }
        }

        void SpaceTimer()
        {
            timeText.text = time.ToString();
            time = time - 1;
            if(time == 0) {
                time = 3;
            }
        }

        void hideHint()
        {
            hintText.text = "";
        }


        void updateHint()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            string name = currentScene.name;

            switch(name)
            {
                case "Level0":
                hintText.text = "day 1";
                break;
                case "hops-1":
                hintText.text = "day 1: all out of hops";
                break;
                case "TimeWorld-1":
                hintText.text = "day 1: reverse reverse every 10 seconds";
                break;
                case "fishes-1":
                hintText.text = "day 1: blobs are friends not food";
                break;
                case "space-1":
                hintText.text = "topsy turvy";
                break;
                case "space-2":
                hintText.text = "";
                break;
                case "space-3":
                hintText.text = "the upside down";
                break;
                case "space-4":
                hintText.text = "flip flip";
                break;
                case "TimeWorld-2":
                hintText.text = "day 2: gotta go fast, faster, fastest";
                break;
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
