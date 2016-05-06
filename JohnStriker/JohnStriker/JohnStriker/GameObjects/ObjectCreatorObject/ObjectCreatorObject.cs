﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Input;

namespace JohnStriker.GameObjects.ObjectCreatorObject
{
  
    public class ObjectCreatorObject : GameObject
    {
        private readonly IServiceLocator _services;
        private readonly IInputService _inputService;
        private readonly IGameObjectService _gameObjectService;

        public ObjectCreatorObject(IServiceLocator service)
        {
            _services = service;
            _inputService = service.GetInstance<IInputService>();
            _gameObjectService = service.GetInstance<IGameObjectService>();
        }

        //onupdate is called once per frame
        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (_inputService.IsPressed(Keys.D1, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 1));
            }
            else if (_inputService.IsPressed(Keys.D2, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 2));
            }
            else if (_inputService.IsPressed(Keys.D3, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 3));
            }
            else if (_inputService.IsPressed(Keys.D4, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 4));
            }
            else if (_inputService.IsPressed(Keys.D5, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 5));
            }
            else if (_inputService.IsPressed(Keys.D6, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 6));
            }
            else if (_inputService.IsPressed(Keys.D7, true))
            {
                _gameObjectService.Objects.Add(new DynamicObject.DynamicObject(_services, 7));
            }
            else if (_inputService.IsPressed(Keys.D8, true))
            {
                var lavaBallsObject = _gameObjectService.Objects.OfType<LavaBallsObject.LavaBallsObject>().FirstOrDefault();
                if (lavaBallsObject != null)
                    lavaBallsObject.Spawn();
            }
            else if (_inputService.IsPressed(Keys.D9, true))
            {
              //  _gameObjectService.Objects.Add(new ProceduralObject(_services));
            }

            base.OnUpdate(deltaTime);
        }
    }
}
