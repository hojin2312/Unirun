using System;
using UnityEngine;

// Controls the menu made in the scene. UI layout remains editable in the Canvas.
public class MainMenuController : MonoBehaviour
{
    [Serializable]
    public class ThemeOption
    {
        public string name;
        public Sprite sky;
        public Sprite startingPlatform;
        public Sprite platform;
        public Sprite obstacle;
        public PlayerEffects.EffectStyle effects = new PlayerEffects.EffectStyle();
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private PlatformSpawner platformSpawner;
    [SerializeField] private SpriteRenderer[] backgrounds = Array.Empty<SpriteRenderer>();
    [SerializeField] private SpriteRenderer startingPlatform;
    [SerializeField] private RuntimeAnimatorController[] characters = Array.Empty<RuntimeAnimatorController>();
    [SerializeField] private ThemeOption[] themes = Array.Empty<ThemeOption>();

    private GameManager gameManager;
    private int characterIndex;
    private int themeIndex;

    // Keep the choices when a game-over reloads this scene, for this play session.
    private static int selectedCharacter;
    private static int selectedTheme;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        characterIndex = Mathf.Clamp(selectedCharacter, 0, characters.Length - 1);
        themeIndex = Mathf.Clamp(selectedTheme, 0, themes.Length - 1);
        ApplyCharacter();
        ApplyTheme();
    }

    public void NextCharacter()
    {
        if (!CanChangeSelection() || characters.Length == 0) return;
        characterIndex = (characterIndex + 1) % characters.Length;
        selectedCharacter = characterIndex;
        ApplyCharacter();
    }

    public void NextTheme()
    {
        if (!CanChangeSelection() || themes.Length == 0) return;
        themeIndex = (themeIndex + 1) % themes.Length;
        selectedTheme = themeIndex;
        ApplyTheme();
    }

    private bool CanChangeSelection()
    {
        return gameManager != null && !gameManager.IsPlaying && !gameManager.isGameover;
    }

    private void ApplyCharacter()
    {
        if (player != null && characters.Length > 0)
            player.PreviewCharacter(characters[characterIndex]);
    }

    private void ApplyTheme()
    {
        if (themes.Length == 0) return;
        var theme = themes[themeIndex];
        foreach (var background in backgrounds)
        {
            if (background != null) background.sprite = theme.sky;
        }
        if (startingPlatform != null) startingPlatform.sprite = theme.startingPlatform;
        if (platformSpawner != null) platformSpawner.SetTheme(theme.platform, theme.obstacle);
        if (player != null && player.TryGetComponent<PlayerEffects>(out var effects))
            effects.SetStyle(theme.effects);
    }
}
