using System.Collections.Generic;
using HexTactics.Grid;
using HexTactics.Pathfinding;
using HexTactics.Turns;
using HexTactics.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HexTactics.Input
{
    public class HexGridInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private TurnManager turnManager;

        [Header("Raycast")]
        [SerializeField] private LayerMask raycastLayerMask = ~0;
        [SerializeField, Min(1f)] private float raycastDistance = 500f;

        private HexTile hoveredTile;
        private TacticalUnit selectedUnit;

        private readonly List<HexTile> movementHighlights = new();
        private readonly List<HexTile> attackHighlights = new();

        private readonly HashSet<HexTile> reachableTiles = new();
        private readonly HashSet<TacticalUnit> attackableUnits = new();

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (turnManager == null)
            {
                turnManager = FindFirstObjectByType<TurnManager>();
            }
        }

        private void Update()
        {
            UpdateHoveredTile();

            if (Mouse.current == null ||
                Keyboard.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleLeftClick();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                RefreshSelectedUnitVisuals();
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                EndCurrentActivation();
            }
        }

        private void HandleLeftClick()
        {
            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return;
            }

            TacticalUnit clickedUnit =
                hit.collider.GetComponentInParent<TacticalUnit>();

            if (clickedUnit != null)
            {
                if (selectedUnit != null &&
                    attackableUnits.Contains(clickedUnit))
                {
                    AttackUnit(clickedUnit);
                    return;
                }

                SelectUnit(clickedUnit);
                return;
            }

            HexTile clickedTile =
                hit.collider.GetComponent<HexTile>();

            if (clickedTile != null &&
                selectedUnit != null &&
                reachableTiles.Contains(clickedTile))
            {
                MoveSelectedUnit(clickedTile);
            }
        }

        private void SelectUnit(TacticalUnit unit)
        {
            if (turnManager == null || unit == null)
            {
                return;
            }

            if (!turnManager.BeginActivation(unit))
            {
                Debug.Log(
                    $"Cannot activate {unit.name}. " +
                    $"Current team: {turnManager.ActiveTeam}"
                );

                return;
            }

            ClearAllHighlights();

            selectedUnit = unit;
            ShowAvailableActions();
        }

        private void ShowAvailableActions()
        {
            if (selectedUnit == null ||
                selectedUnit.CurrentTile == null ||
                !selectedUnit.HasActionPoints())
            {
                return;
            }

            ShowMovementRange();
            ShowAttackRange();

            selectedUnit.CurrentTile.SetSelected(true);
        }

        private void ShowMovementRange()
        {
            Dictionary<HexTile, int> results =
                HexPathfinder.GetReachableTiles(
                    selectedUnit.CurrentTile,
                    selectedUnit.MovementPoints
                );

            foreach (KeyValuePair<HexTile, int> result in results)
            {
                HexTile tile = result.Key;

                if (tile == selectedUnit.CurrentTile)
                {
                    continue;
                }

                reachableTiles.Add(tile);
                movementHighlights.Add(tile);
                tile.SetMovementHighlight(true);
            }
        }

        private void ShowAttackRange()
        {
            TacticalUnit[] allUnits =
                FindObjectsByType<TacticalUnit>(
                    FindObjectsSortMode.None
                );

            foreach (TacticalUnit unit in allUnits)
            {
                if (!selectedUnit.CanAttack(unit))
                {
                    continue;
                }

                attackableUnits.Add(unit);

                if (unit.CurrentTile != null)
                {
                    attackHighlights.Add(unit.CurrentTile);
                    unit.CurrentTile.SetAttackHighlight(true);
                }
            }
        }

        private void MoveSelectedUnit(HexTile destinationTile)
        {
            if (selectedUnit == null ||
                selectedUnit.IsMoving ||
                !selectedUnit.HasActionPoints())
            {
                return;
            }

            List<HexTile> path =
                HexPathfinder.FindPath(
                    selectedUnit.CurrentTile,
                    destinationTile
                );

            if (path == null)
            {
                return;
            }

            int pathCost =
                HexPathfinder.GetPathCost(path);

            if (pathCost > selectedUnit.MovementPoints)
            {
                return;
            }

            selectedUnit.CurrentTile.SetSelected(false);
            ClearAllHighlights();

            bool started = selectedUnit.MoveAlongPath(
                path,
                HandleActionFinished
            );

            if (!started)
            {
                ShowAvailableActions();
            }
        }

        private void AttackUnit(TacticalUnit target)
        {
            if (selectedUnit == null ||
                selectedUnit.IsMoving)
            {
                return;
            }

            ClearAllHighlights();

            bool attacked = selectedUnit.Attack(target);

            if (!attacked)
            {
                ShowAvailableActions();
                return;
            }

            HandleActionFinished();
        }

        private void HandleActionFinished()
        {
            if (selectedUnit == null)
            {
                return;
            }

            if (!selectedUnit.HasActionPoints())
            {
                selectedUnit.CurrentTile?.SetSelected(false);

                ClearAllHighlights();

                selectedUnit = null;
                turnManager.EndCurrentActivation();
                return;
            }

            ShowAvailableActions();
        }

        private void EndCurrentActivation()
        {
            if (turnManager == null ||
                turnManager.ActiveUnit == null ||
                turnManager.ActiveUnit.IsMoving)
            {
                return;
            }

            if (selectedUnit != null &&
                selectedUnit.CurrentTile != null)
            {
                selectedUnit.CurrentTile.SetSelected(false);
            }

            selectedUnit = null;
            ClearAllHighlights();

            turnManager.EndCurrentActivation();
        }

        private void RefreshSelectedUnitVisuals()
        {
            if (selectedUnit == null ||
                selectedUnit.IsMoving)
            {
                return;
            }

            ClearAllHighlights();
            ShowAvailableActions();
        }

        private void ClearAllHighlights()
        {
            foreach (HexTile tile in movementHighlights)
            {
                if (tile != null)
                {
                    tile.SetMovementHighlight(false);
                }
            }

            foreach (HexTile tile in attackHighlights)
            {
                if (tile != null)
                {
                    tile.SetAttackHighlight(false);
                }
            }

            movementHighlights.Clear();
            attackHighlights.Clear();
            reachableTiles.Clear();
            attackableUnits.Clear();
        }

        private void UpdateHoveredTile()
        {
            HexTile newHoveredTile = GetTileUnderMouse();

            if (newHoveredTile == hoveredTile)
            {
                return;
            }

            if (hoveredTile != null)
            {
                hoveredTile.SetHover(false);
            }

            hoveredTile = newHoveredTile;

            if (hoveredTile != null)
            {
                hoveredTile.SetHover(true);
            }
        }

        private HexTile GetTileUnderMouse()
        {
            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return null;
            }

            HexTile tile =
                hit.collider.GetComponent<HexTile>();

            if (tile != null)
            {
                return tile;
            }

            TacticalUnit unit =
                hit.collider.GetComponentInParent<TacticalUnit>();

            return unit != null ? unit.CurrentTile : null;
        }

        private bool TryGetObjectUnderMouse(
            out RaycastHit hit)
        {
            hit = default;

            if (mainCamera == null ||
                Mouse.current == null)
            {
                return false;
            }

            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            Ray ray =
                mainCamera.ScreenPointToRay(mousePosition);

            return Physics.Raycast(
                ray,
                out hit,
                raycastDistance,
                raycastLayerMask
            );
        }
    }
}