public partial class GameState
{
    // Day, turn, movement, and game-over progression.
    public void NextDay()
    {
        day++;
        turn = GetFirstPlayerInTurnOrder();
        movesLeft = ActionsPerTurn;
        NormalizeDayAfterAdvance();

        if (day >= finishDay)
        {
            isGameOver = true;
            return;
        }
    }

    public void SetDay(int targetDay)
    {
        day = targetDay < 1 ? 1 : targetDay;
        turn = GetFirstPlayerInTurnOrder();
        movesLeft = ActionsPerTurn;
        NormalizeDayAfterAdvance();
        isGameOver = day >= finishDay;
    }

    public void SetTurnAndMoves(int targetTurn, int targetMovesLeft)
    {
        turn = UnityEngine.Mathf.Clamp(targetTurn, 1, playerCount);
        movesLeft = UnityEngine.Mathf.Max(0, targetMovesLeft);
    }

    public void UseMove()
    {
        if (movesLeft > 0)
        {
            movesLeft--;
        }
        if (movesLeft <= 0)
        {
            if (IsLastPlayerInTurnOrder(turn))
            {
                turn = GetFirstPlayerInTurnOrder();
                NextDay();
                return;
            }

            turn = GetNextPlayerInTurnOrder(turn);
            movesLeft = ActionsPerTurn;
        }
    }

    public void ConsumeMoveWithoutTurnProgress()
    {
        if (movesLeft > 0)
        {
            movesLeft--;
        }
    }

    public void AdvanceTurnIfMovesDepleted()
    {
        if (movesLeft > 0)
        {
            return;
        }

        if (IsLastPlayerInTurnOrder(turn))
        {
            turn = GetFirstPlayerInTurnOrder();
            NextDay();
            return;
        }

        turn = GetNextPlayerInTurnOrder(turn);
        movesLeft = ActionsPerTurn;
    }

    public bool IsJumatBerkah()
    {
        return FridayEnabled && GetDayOfWeek(day) == 5;
    }

    public bool IsInvestasiEmasDay()
    {
        return SaturdayEnabled && GetDayOfWeek(day) == 6;
    }

    public bool IsHariMingguLibur()
    {
        return SundayIsHoliday && GetDayOfWeek(day) == 7;
    }

    // Minggu libur dilewati seluruhnya: tidak ada giliran pemain pada hari itu.
    public void LewatiHariMinggu()
    {
        if (!IsHariMingguLibur())
        {
            return;
        }

        NextDay();
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    private void NormalizeDayAfterAdvance()
    {
        while (!SundayEnabled && GetDayOfWeek(day) == 7 && day < finishDay)
        {
            day++;
        }
    }

    private static int GetDayOfWeek(int value)
    {
        int mod = (value - 1) % 7;
        return mod < 0 ? mod + 8 : mod + 1;
    }
}
