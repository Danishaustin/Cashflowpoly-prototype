public partial class GameState
{
    // Day, turn, movement, and game-over progression.
    public void NextDay()
    {
        day++;
        turn = 1;
        movesLeft = 2;
        if (day >= finishDay)
        {
            isGameOver = true;
            return;
        }
        while (day % 7 == 0)
        {
            day++;
        }
    }

    public void SetDay(int targetDay)
    {
        day = targetDay < 1 ? 1 : targetDay;
        turn = 1;
        movesLeft = 2;
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
            if (turn == playerCount)
            {
                turn = 1;
                NextDay();
                return;
            }

            turn++;
            movesLeft = 2;
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

        if (turn == playerCount)
        {
            turn = 1;
            NextDay();
            return;
        }

        turn++;
        movesLeft = 2;
    }

    public bool IsJumatBerkah()
    {
        if (day % 7 == 5)
        {
            return true;
        }

        return false;
    }

    public bool IsInvestasiEmasDay()
    {
        return day % 7 == 6;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }
}
