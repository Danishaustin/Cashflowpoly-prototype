using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Pause panel actions and scene navigation.
    private void TogglePause(ClickEvent evt)
    {
        SetPauseState(!GameState.Instance.isPaused);
    }


    private void ResumePause(ClickEvent evt)
    {
        SetPauseState(false);
    }

    private void RestartGame(ClickEvent evt)
    {
        SetPauseState(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToHome(ClickEvent evt)
    {
        SetPauseState(false);
        SceneManager.LoadScene(0);
    }

    private void SetPauseState(bool pause)
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.SetPause(pause);
        }

        Time.timeScale = pause ? 0f : 1f;

        if (pausePanel == null)
        {
            return;
        }

        if (pause)
        {
            if (pauseInputBlocker != null)
            {
                pauseInputBlocker.style.display = DisplayStyle.Flex;
                pauseInputBlocker.BringToFront();
            }

            pauseToggleButton?.BringToFront();
            pausePanel.style.display = DisplayStyle.Flex;
            pausePanel.BringToFront();
            pausePanel.schedule.Execute(() =>
            {
                pausePanel.AddToClassList("show-pause-panel");
            }).StartingIn(1);
            return;
        }

        pausePanel.RemoveFromClassList("show-pause-panel");
        pausePanel.schedule.Execute(() =>
        {
            if (GameState.Instance == null || !GameState.Instance.isPaused)
            {
                pausePanel.style.display = DisplayStyle.None;

                if (pauseInputBlocker != null)
                {
                    pauseInputBlocker.style.display = DisplayStyle.None;
                }
            }
        }).StartingIn(350);
    }
}
