using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Apos.Input;
using FontStashSharp;

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";

            _settings = EnsureJson<Settings>("Settings.json");
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            IsFixedTimeStep = _settings.IsFixedTimeStep;
            Window.Position = new Point(_settings.X, _settings.Y);
            _graphics.SynchronizeWithVerticalRetrace = _settings.IsVSync;
            _graphics.PreferredBackBufferWidth = _settings.Width;
            _graphics.PreferredBackBufferHeight = _settings.Height;

            _settings.IsFullscreen = _settings.IsFullscreen || _settings.IsBorderless;

            if (_settings.IsFullscreen) {
                SetFullscreen();
            } else {
                _graphics.ApplyChanges();
            }

            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);

            InputHelper.Setup(this);

            _fontSystem = new FontSystem();
            _fontSystem.AddFont(TitleContainer.OpenStream($"{Content.RootDirectory}/source-code-pro-medium.ttf"));
        }

        protected override void UnloadContent() {
            if (!_settings.IsFullscreen) {
                SaveWindow();
            }

            SaveJson<Settings>("Settings.json", _settings);

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (_quit.Pressed())
                Exit();

            if (_toggleFullscreen.Pressed()) {
                ToggleFullscreen();
            }
            if (_toggleBorderless.Pressed()) {
                ToggleBorderless();
            }

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            var font = _fontSystem.GetFont(24);
            string mode = _settings.IsBorderless ? "Borderless" : _settings.IsFullscreen ? "Fullscreen" : "Window";
            Vector2 modeCenter = font.MeasureString(mode) / 2f;
            Vector2 windowCenter = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2f;

            _s.Begin();
            _s.DrawString(font, mode, windowCenter - modeCenter, Color.White);
            _s.End();

            base.Draw(gameTime);
        }

        private string RootPath => AppDomain.CurrentDomain.BaseDirectory;
        private string GetPath(string name) => Path.Combine(RootPath, name);
        private T LoadJson<T>(string name) where T : new() {
            T json;
            string jsonPath = GetPath(name);

            if (File.Exists(jsonPath)) {
                var options = new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                json = JsonSerializer.Deserialize<T>(File.ReadAllText(jsonPath), options);
            } else {
                json = new T();
            }

            return json;
        }
        private T EnsureJson<T>(string name) where T : new() {
            T json;
            string jsonPath = GetPath(name);

            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            if (File.Exists(jsonPath)) {
                json = JsonSerializer.Deserialize<T>(File.ReadAllText(jsonPath), options);
            } else {
                json = new T();
                string jsonString = JsonSerializer.Serialize(json, options);
                File.WriteAllText(jsonPath, jsonString);
            }

            return json;
        }
        private void SaveJson<T>(string name, T json) {
            string jsonPath = GetPath(name);
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
            string jsonString = JsonSerializer.Serialize(json, options);
            File.WriteAllText(jsonPath, jsonString);
        }

        private void ToggleFullscreen() {
            bool oldFullscreen = _settings.IsFullscreen;

            if (_settings.IsBorderless) {
                _settings.IsBorderless = false;
            } else {
                _settings.IsFullscreen = !_settings.IsFullscreen;
            }

            if (_settings.IsFullscreen) {
                if (oldFullscreen) {
                    ApplyHardwareMode();
                } else {
                    SetFullscreen();
                }
            } else {
                UnsetFullscreen();
            }
        }
        private void ToggleBorderless() {
            bool oldFullscreen = _settings.IsFullscreen;

            _settings.IsBorderless = !_settings.IsBorderless;
            _settings.IsFullscreen = _settings.IsBorderless;

            if (_settings.IsFullscreen) {
                if (oldFullscreen) {
                    ApplyHardwareMode();
                } else {
                    SetFullscreen();
                }
            } else {
                UnsetFullscreen();
            }
        }

        private void ApplyHardwareMode() {
            _graphics.HardwareModeSwitch = !_settings.IsBorderless;
            _graphics.ApplyChanges();
        }
        private void SetFullscreen() {
            SaveWindow();

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.HardwareModeSwitch = !_settings.IsBorderless;

            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        private void UnsetFullscreen() {
            RestoreWindow();

            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
        }
        private void SaveWindow() {
            _settings.X = Window.ClientBounds.X;
            _settings.Y = Window.ClientBounds.Y;
            _settings.Width = Window.ClientBounds.Width;
            _settings.Height = Window.ClientBounds.Height;
        }
        private void RestoreWindow() {
            Window.Position = new Point(_settings.X, _settings.Y);
            _graphics.PreferredBackBufferWidth = _settings.Width;
            _graphics.PreferredBackBufferHeight = _settings.Height;
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;
        FontSystem _fontSystem;

        Settings _settings;

        ICondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );
        ICondition _toggleFullscreen =
            new AllCondition(
                new KeyboardCondition(Keys.LeftAlt),
                new KeyboardCondition(Keys.Enter)
            );
        ICondition _toggleBorderless = new KeyboardCondition(Keys.F11);
    }
}
