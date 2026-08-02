using System.Collections;
using System.Collections.Generic;
using HexTactics.Units;
using UnityEngine;

namespace HexTactics.Turns
{
    public class TurnManager : MonoBehaviour
    {
        [Header("Current Turn")]
        [SerializeField] private Team activeTeam = Team.PlayerOne;
        [SerializeField] private int roundNumber = 1;

        private readonly List<TacticalUnit> units = new();

        public Team ActiveTeam => activeTeam;
        public int RoundNumber => roundNumber;
        public TacticalUnit ActiveUnit { get; private set; }

        private IEnumerator Start()
        {
            // Wait one frame so all units can place themselves first.
            yield return null;

            RefreshUnitList();
            BeginRound();
        }

        public bool CanSelectUnit(TacticalUnit unit)
        {
            if (unit == null || unit.IsDead || unit.IsMoving)
            {
                return false;
            }

            if (ActiveUnit != null && ActiveUnit != unit)
            {
                return false;
            }

            return unit.UnitTeam == activeTeam &&
                   !unit.HasActivated;
        }

        public bool BeginActivation(TacticalUnit unit)
        {
            if (!CanSelectUnit(unit))
            {
                return false;
            }

            if (ActiveUnit == null)
            {
                ActiveUnit = unit;
                ActiveUnit.BeginActivation();

                Debug.Log(
                    $"{activeTeam} activated {ActiveUnit.name}. " +
                    $"AP: {ActiveUnit.CurrentActionPoints}"
                );
            }

            return true;
        }

        public void EndCurrentActivation()
        {
            if (ActiveUnit == null || ActiveUnit.IsMoving)
            {
                return;
            }

            TacticalUnit completedUnit = ActiveUnit;

            completedUnit.EndActivation();
            ActiveUnit = null;

            Debug.Log($"{completedUnit.name} finished activating.");

            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            Team otherTeam = activeTeam == Team.PlayerOne
                ? Team.PlayerTwo
                : Team.PlayerOne;

            if (HasAvailableUnit(otherTeam))
            {
                activeTeam = otherTeam;
            }
            else if (HasAvailableUnit(activeTeam))
            {
                // The other player has no units left to activate.
                // The current player continues.
            }
            else
            {
                roundNumber++;
                BeginRound();
                return;
            }

            Debug.Log(
                $"Round {roundNumber}: {activeTeam} chooses a unit."
            );
        }

        private void BeginRound()
        {
            RefreshUnitList();

            foreach (TacticalUnit unit in units)
            {
                if (unit != null)
                {
                    unit.ResetForNewRound();
                }
            }

            activeTeam = Team.PlayerOne;
            ActiveUnit = null;

            Debug.Log($"Round {roundNumber} started. Player One acts first.");
        }

        private bool HasAvailableUnit(Team team)
        {
            foreach (TacticalUnit unit in units)
            {
                if (unit != null &&
                    !unit.IsDead &&
                    unit.UnitTeam == team &&
                    !unit.HasActivated)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshUnitList()
        {
            units.Clear();

            TacticalUnit[] foundUnits =
                FindObjectsByType<TacticalUnit>(
                    FindObjectsSortMode.None
                );

            units.AddRange(foundUnits);
            units.RemoveAll(unit => unit == null);
        }
    }
}