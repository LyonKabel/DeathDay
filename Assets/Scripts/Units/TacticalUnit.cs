using System;
using System.Collections;
using System.Collections.Generic;
using HexTactics.Grid;
using HexTactics.Turns;
using UnityEngine;

namespace HexTactics.Units
{
    public class TacticalUnit : MonoBehaviour
    {
        [Header("Unit Ownership")]
        [SerializeField] private Team unitTeam = Team.PlayerOne;

        [Header("Starting Position")]
        [SerializeField] private int startingColumn = 4;
        [SerializeField] private int startingRow = 3;

        [Header("Health")]
        [SerializeField, Min(1)] private int maximumHealth = 10;

        [Header("Actions")]
        [SerializeField, Min(1)] private int maximumActionPoints = 2;

        [Header("Movement")]
        [SerializeField, Min(1)] private int movementPoints = 4;
        [SerializeField, Min(0.1f)] private float movementSpeed = 4f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 10f;

        [Header("Attack")]
        [SerializeField, Min(1)] private int attackRange = 3;
        [SerializeField, Min(1)] private int attackDamage = 4;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float heightOffset = 0.6f;

        private HexGridManager gridManager;

        public Team UnitTeam => unitTeam;
        public HexTile CurrentTile { get; private set; }

        public int MovementPoints => movementPoints;
        public int AttackRange => attackRange;
        public int AttackDamage => attackDamage;

        public int CurrentHealth { get; private set; }
        public int CurrentActionPoints { get; private set; }

        public bool IsMoving { get; private set; }
        public bool HasActivated { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private void Awake()
        {
            CurrentHealth = maximumHealth;
        }

        private void Start()
        {
            gridManager = FindFirstObjectByType<HexGridManager>();

            if (gridManager == null)
            {
                Debug.LogError("No HexGridManager was found.");
                return;
            }

            if (!gridManager.TryGetTile(
                    startingColumn,
                    startingRow,
                    out HexTile startingTile))
            {
                Debug.LogError(
                    $"Starting tile ({startingColumn}, {startingRow}) does not exist."
                );

                return;
            }

            PlaceOnTile(startingTile);
        }

        public void ResetForNewRound()
        {
            if (IsDead)
            {
                return;
            }

            HasActivated = false;
            CurrentActionPoints = 0;
        }

        public void BeginActivation()
        {
            if (IsDead)
            {
                return;
            }

            CurrentActionPoints = maximumActionPoints;
        }

        public void EndActivation()
        {
            CurrentActionPoints = 0;
            HasActivated = true;
        }

        public bool HasActionPoints(int requiredAmount = 1)
        {
            return !IsDead &&
                   CurrentActionPoints >= requiredAmount;
        }

        public bool SpendActionPoints(int amount)
        {
            if (amount <= 0 ||
                CurrentActionPoints < amount ||
                IsDead)
            {
                return false;
            }

            CurrentActionPoints -= amount;
            return true;
        }

        public bool CanAttack(TacticalUnit target)
        {
            if (target == null ||
                target == this ||
                target.IsDead ||
                target.UnitTeam == UnitTeam ||
                CurrentTile == null ||
                target.CurrentTile == null ||
                !HasActionPoints())
            {
                return false;
            }

            int distance = HexGridUtility.GetDistance(
                CurrentTile,
                target.CurrentTile
            );

            return distance <= attackRange;
        }

        public bool Attack(TacticalUnit target)
        {
            if (!CanAttack(target))
            {
                return false;
            }

            if (!SpendActionPoints(1))
            {
                return false;
            }

            Vector3 direction =
                target.transform.position -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }

            target.TakeDamage(attackDamage);

            Debug.Log(
                $"{name} attacked {target.name} for " +
                $"{attackDamage} damage. " +
                $"{target.name} has {target.CurrentHealth} health remaining."
            );

            return true;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0)
            {
                return;
            }

            CurrentHealth =
                Mathf.Max(0, CurrentHealth - damage);

            if (CurrentHealth == 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{name} was defeated.");

            if (CurrentTile != null)
            {
                CurrentTile.SetOccupyingUnit(null);
                CurrentTile = null;
            }

            gameObject.SetActive(false);
        }

        public bool PlaceOnTile(HexTile destinationTile)
        {
            if (destinationTile == null ||
                !destinationTile.IsWalkable ||
                IsDead)
            {
                return false;
            }

            if (destinationTile.IsOccupied &&
                destinationTile.OccupyingUnit != gameObject)
            {
                return false;
            }

            if (CurrentTile != null)
            {
                CurrentTile.SetOccupyingUnit(null);
            }

            CurrentTile = destinationTile;
            CurrentTile.SetOccupyingUnit(gameObject);

            transform.position =
                destinationTile.transform.position +
                Vector3.up * heightOffset;

            return true;
        }

        public bool MoveAlongPath(
            List<HexTile> path,
            Action onMovementFinished)
        {
            if (IsMoving ||
                IsDead ||
                path == null ||
                path.Count < 2 ||
                !HasActionPoints())
            {
                return false;
            }

            if (!SpendActionPoints(1))
            {
                return false;
            }

            StartCoroutine(
                MoveRoutine(path, onMovementFinished)
            );

            return true;
        }

        private IEnumerator MoveRoutine(
            List<HexTile> path,
            Action onMovementFinished)
        {
            IsMoving = true;

            HexTile startingTile = CurrentTile;
            HexTile destinationTile = path[^1];

            startingTile.SetOccupyingUnit(null);

            for (int i = 1; i < path.Count; i++)
            {
                HexTile nextTile = path[i];

                Vector3 destinationPosition =
                    nextTile.transform.position +
                    Vector3.up * heightOffset;

                while (Vector3.Distance(
                           transform.position,
                           destinationPosition) > 0.01f)
                {
                    Vector3 movementDirection =
                        destinationPosition -
                        transform.position;

                    Vector3 flatDirection = new(
                        movementDirection.x,
                        0f,
                        movementDirection.z
                    );

                    if (flatDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation =
                            Quaternion.LookRotation(flatDirection);

                        transform.rotation =
                            Quaternion.Slerp(
                                transform.rotation,
                                targetRotation,
                                rotationSpeed * Time.deltaTime
                            );
                    }

                    transform.position =
                        Vector3.MoveTowards(
                            transform.position,
                            destinationPosition,
                            movementSpeed * Time.deltaTime
                        );

                    yield return null;
                }

                transform.position = destinationPosition;
                CurrentTile = nextTile;
            }

            CurrentTile = destinationTile;
            CurrentTile.SetOccupyingUnit(gameObject);

            IsMoving = false;
            onMovementFinished?.Invoke();
        }
    }
}