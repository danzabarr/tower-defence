﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum Type
    {
        Direct,
        FixedPitch,
        FixedVelocity,
        FixedVelocityLob
    }

    [SerializeField]
    private float life;

    [SerializeField]
    private Type type;

    [SerializeField]
    private float velocity;

    [SerializeField]
    private float pitch;

    [SerializeField]
    private LayerMask explosionMask;

    [SerializeField]
    private ParticleSystem explosionParticleSystem;

    [SerializeField]
    private float explosionForce;

    [SerializeField]
    private float explosionDamage;

    [SerializeField]
    private float explosionRange;

    private Rigidbody rb;
    private Vector3 target;
    private bool hit;

    public void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Update()
    {
        if (life <= -1)
            return;

        life -= Time.deltaTime;
        life = Mathf.Max(0, life);
        if (life <= 0)
        {
            OnEndLife();
            Destroy(gameObject);
        }
    }

    public virtual void OnEndLife()
    {

    }

    public virtual void OnHit()
    {
        Game.Instance.AddForce(transform.position, explosionForce, explosionRange);
        Game.Instance.AddDamage(transform.position, explosionDamage, explosionRange);
        if (explosionParticleSystem)
            Instantiate(explosionParticleSystem, transform.position, Quaternion.identity);

        AudioManager.Instance.Play("Explosion");
    }

    public bool Fire(Vector3 target)
    {
        this.target = target;

        if (type == Type.Direct)
        {
            rb.useGravity = false;
        }
        else if (type == Type.FixedPitch)
        {
            rb.useGravity = true;
            if (Ballistics.SolveArcPitch(transform.position, target, pitch, out Vector3 projectileVelocity))
            {
                rb.velocity = projectileVelocity;
                return true;
            }
        }
        else
        {
            rb.useGravity = true;
            int sav = Ballistics.SolveArcVector(transform.position, velocity, target, -Physics.gravity.y, out Vector3 s0, out Vector3 s1);
            if (sav == 1)
            {
                rb.velocity = s0;
                return true;
            }
            else if (sav == 2)
            {
                if (type == Type.FixedVelocity)
                {
                    rb.velocity = s0;
                    return true;
                }
                else if (type == Type.FixedVelocityLob)
                {
                    rb.velocity = s1;
                    return true;
                }
                return false;
            }
            else
                return false;
        }
        return false;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (hit)
            return;
        bool inMask = explosionMask == (explosionMask | (1 << collision.gameObject.layer));
        if (inMask)
        {
            hit = true;
            OnHit();
            Destroy(gameObject);
        }
    }
}
