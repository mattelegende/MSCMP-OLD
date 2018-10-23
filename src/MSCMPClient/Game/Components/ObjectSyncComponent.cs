﻿using UnityEngine;
using MSCMP.Network;
using MSCMP.Game.Objects;

namespace MSCMP.Game.Components {
	/// <summary>
	/// Attached to objects that require position/rotation sync.
	/// Sync is provided based on distance from the player and paramters inside an ISyncedObject.
	/// </summary>
	class ObjectSyncComponent : MonoBehaviour {
		// If sync is enabled.
		public bool SyncEnabled = false;
		// Sync owner.
		public ulong Owner = 0;
		// Object ID.
		public int ObjectID = ObjectSyncManager.AUTOMATIC_ID;
		// Object type.
		public ObjectSyncManager.ObjectTypes ObjectType;

		// True if sync should be sent continuously, ignore syncedObject.CanSync()
		bool sendConstantSync = false;
		// Transform of synced object.
		Transform objectTransform;
		// The synced object.
		ISyncedObject syncedObject;

		/// <summary>
		/// Ran on script enable.
		/// </summary>
		void Start() {
			Logger.Debug($"Sync component added to: {this.transform.name}");

			// Assign object's ID.
			ObjectID = ObjectSyncManager.Instance.AddNewObject(this, ObjectID);

			// Set object type.
			switch (ObjectType) {
				// Pickupable.
				case ObjectSyncManager.ObjectTypes.Pickupable:
					syncedObject = new Pickupable(this.gameObject);
					break;
				// AI Vehicle.
				case ObjectSyncManager.ObjectTypes.AIVehicle:
					syncedObject = new AIVehicle(this.gameObject, this);
					break;
			}

			objectTransform = syncedObject.ObjectTransform();
		}

		/// <summary>
		/// Called once per frame.
		/// </summary>
		void Update() {
			if (SyncEnabled) {
				// Updates object's position continuously.
				// (Typically used when player is holding an pickupable, or driving a vehicle)
				if (sendConstantSync) {
					SendObjectSync(ObjectSyncManager.SyncTypes.GenericSync, true);
				}

				// Check if object should be synced.
				else if (syncedObject.CanSync()) {
					SendObjectSync(ObjectSyncManager.SyncTypes.GenericSync, true);
				}
			}

			// Periodically update the object's position if periodic sync is enabled.
			if (syncedObject.PeriodicSyncEnabled() && ObjectSyncManager.Instance.ShouldPeriodicSync(Owner, SyncEnabled)) {
				SendObjectSync(ObjectSyncManager.SyncTypes.PeriodicSync, true);
			}
		}

		/// <summary>
		/// Sends a sync update of the object.
		/// </summary>
		public void SendObjectSync(ObjectSyncManager.SyncTypes type, bool sendVariables) {
			if (sendVariables) {
				NetLocalPlayer.Instance.SendObjectSync(ObjectID, objectTransform.position, objectTransform.rotation, type, syncedObject.ReturnSyncedVariables());
			}
			else {
				NetLocalPlayer.Instance.SendObjectSync(ObjectID, objectTransform.position, objectTransform.rotation, type, null);
			}
		}

		/// <summary>
		/// Request a sync update from the host.
		/// </summary>
		public void RequestObjectSync() {
			NetLocalPlayer.Instance.RequestObjectSync(ObjectID);
		}

		/// <summary>
		/// Called when object sync request is accepted by the remote client.
		/// </summary>
		public void SyncRequestAccepted() {
			Owner = Steamworks.SteamUser.GetSteamID().m_SteamID;
			SyncEnabled = true;
		}

		/// <summary>
		/// Called when the player enter sync range of the object.
		/// </summary>
		public void SendEnterSync() {
			if (Owner == ObjectSyncManager.NO_OWNER) {
				SendObjectSync(ObjectSyncManager.SyncTypes.SetOwner, true);
			}
		}

		/// <summary>
		/// Called when the player exits sync range of the object.
		/// </summary>
		public void SendExitSync() {
			if (Owner == ObjectSyncManager.Instance.steamID.m_SteamID) {
				Owner = ObjectSyncManager.NO_OWNER;
				SyncEnabled = false;
				SendObjectSync(ObjectSyncManager.SyncTypes.RemoveOwner, false);
			}
		}

		/// <summary>
		/// Take sync control of the object by force.
		/// </summary>
		public void TakeSyncControl() {
			if (Owner != Steamworks.SteamUser.GetSteamID().m_SteamID) {
				SendObjectSync(ObjectSyncManager.SyncTypes.ForceSetOwner, true);
			}
		}

		/// <summary>
		/// Called when sync owner is set to the remote client.
		/// </summary>
		public void OwnerSetToRemote(ulong newOwner) {
			Owner = newOwner;
			syncedObject.OwnerSetToRemote();
		}

		/// <summary>
		/// Called when sync control of an object has been taken from local player.
		/// </summary>
		public void SyncTakenByForce() {
			syncedObject.SyncTakenByForce();
		}

		/// <summary>
		/// Set object to send position and rotation sync constantly.
		/// </summary>
		/// <param name="newValue">If object should be constantly synced.</param>
		public void SendConstantSync(bool newValue) {
			sendConstantSync = newValue;
			syncedObject.ConstantSyncChanged(newValue);
		}

		/// <summary>
		/// Handles synced variables sent from remote client.
		/// </summary>
		/// <param name="syncedVariables">Synced variables</param>
		public void HandleSyncedVariables(float[] syncedVariables) {
			syncedObject.HandleSyncedVariables(syncedVariables);
		}

		/// <summary>
		/// Set object's postion and rotationn.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		public void SetPositionAndRotation(Vector3 pos, Quaternion rot) {
			objectTransform.position = pos;
			objectTransform.rotation = rot;
		}
	}
}