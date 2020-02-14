using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Netcode;

namespace DEPRECATED
{
	public class Player : Entity
	{
		public enum PlayerState
		{
			Alive = (1 << 0),
			Shooting = (1 << 1),
			Jumping = (1 << 2),
		}

		private Rigidbody playerBody;

		public int activeWeapon = 0;

		static public Vector3 pos = new Vector3(0, 0, 0);
		public float moveSpeed = 1;
		public float maxSpeed = 20.0f;
		public int state = (int)PlayerState.Alive;

		public Weapon[] weapons;

		// Start is called before the first frame update
		protected override void BaseStart()
		{
			type = EntityType.Player;

			currentHealth = 200;
			maxHealth = 200;

			destructable = false;

			playerBody = this.GetComponent<Rigidbody>();

			foreach (Weapon weapon in weapons)
			{
				weapon.gameObject.SetActive(false);
			}
			weapons[0].gameObject.SetActive(true);
		}

		public void SendWeapon(int weaponNum)
		{
			weapons[activeWeapon].gameObject.SetActive(false);
			activeWeapon = weaponNum;
			weapons[activeWeapon].gameObject.SetActive(true);
		}

		public void SendUpdate(Vector3 pos, Vector3 rot, int state)
		{
			if (GameSceneController.Instance.type == PlayerType.FPS)
			{
			}
			else if (GameSceneController.Instance.type == PlayerType.RTS)
			{

				this.GetComponent<Rigidbody>().velocity = (pos - this.transform.position) * 10f;
				this.transform.rotation = Quaternion.Euler(new Vector3(0f, rot.y, 0f));
				this.state = state;


				if ((state & (int)PlayerState.Shooting) > 0)
				{
					if (!weapons[activeWeapon].playing)
					{
						weapons[activeWeapon].StartPlaying();
					}
				}
				else
				{
					if (weapons[activeWeapon].playing)
					{
						weapons[activeWeapon].StopPlaying();
					}
				}

				if ((state & (int)PlayerState.Jumping) > 0)
				{
					Jump();
				}
			}

		}

		private void Jump()
		{
			//anim.Play("Jump");
		}

		// Update is called once per frame
		protected override void BaseUpdate()
		{
			if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.D) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
			{
				playerBody.velocity += new Vector3(1 * moveSpeed, 0, 0);
			}
			if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.A) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
			{
				playerBody.velocity += new Vector3(1 * -moveSpeed, 0, 0);
			}
			if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.W) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
			{
				playerBody.velocity += new Vector3(0, 0, 1 * moveSpeed);
			}
			if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.S) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
			{
				playerBody.velocity += new Vector3(0, 0, 1 * -moveSpeed);
			}

			//anim.SetFloat("Walk", Vector3.Dot(this.GetComponent<Rigidbody>().velocity, transform.forward) / 10);
			//anim.SetFloat("Turn", Vector3.Dot(this.GetComponent<Rigidbody>().velocity, transform.right) / 10);
		}

		public override void OnDeath()
		{
			Debug.Log("Player's Dead");
			if (ResourceManager.ResourceConstants.UNKILLABLEPLAYER)
			{
				this.currentHealth = 200;
			}
			else
			{
				base.OnDeath();
			}
		}

    public override void OnDamage(int dmg, Entity entity)
    {
        NetworkManager.SendPacketDamage(this.id, entity.id, dmg);
    }
}