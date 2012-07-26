﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Delta
{
    internal class EntityHelper
    {
        internal static Dictionary<string, IEntity> _idReferences = new Dictionary<string, IEntity>();

        internal static void AddIDReference(IEntity entity)
        {
            string id = entity.Name.ToLower();
            if (_idReferences.ContainsKey(id)) //if the ID already exists, append a numerical increment
            {
                for (int x = 1; x < int.MaxValue; x++)
                {
                    string newID = id + x;
                    if (!_idReferences.ContainsKey(newID))
                    {
                        id = newID;
                        break;
                    }
                }
            }
            entity.Name = id;
            _idReferences.Add(id, entity);
        }

        internal static void RemoveIDReference(IEntity entity)
        {
            string id = entity.Name.ToLower();
            if (EntityHelper._idReferences.ContainsKey(id))
                EntityHelper._idReferences.Remove(id);
        }
    }

    /// <summary>
    /// Base class for all game entites.
    /// </summary>
    public abstract class Entity : IRecyclable, IImportable, IEntity, IDisposable
    {
        /// <summary>
        /// Retrieves an <see cref="IEntity"/> by it's global name.
        /// </summary>
        /// <param name="name">The name of the <see cref="IEntity"/> to retrieve.</param>
        /// <returns>The <see cref="IEntity"/> with the specified name. Returns null if an <see cref="IEntity"/> with the specified name could not be found.</returns>
        public static IEntity Get(string name)
        {
            name = name.ToLower();
            if (EntityHelper._idReferences.ContainsKey(name))
                return EntityHelper._idReferences[name];
            return null;
        }

        bool _flaggedForRemoval = false;
        
        /// <summary>
        /// Gets the <see cref="IEntityCollection"/> which is responsible for the <see cref="Entity"/>.
        /// </summary>
        [ContentSerializerIgnore]
        protected internal IEntityCollection ParentCollection { get; internal set; }
        IEntityCollection IEntity.ParentCollection //explict interface property allows us to publicly set the ParentCollection from IEntity.
        {
            get { return ParentCollection; }
            set { ParentCollection = value; }
        }

        /// <summary>
        /// Gets the name of the <see cref="Entity"/>.
        /// </summary>
        [ContentSerializer]
        public string Name { get; internal set; }
        string IEntity.Name //explict interface property allows us to publicly set the Name from IEntity.
        {
            get { return Name; }
            set { Name = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> has initialized.
        /// </summary>
        [ContentSerializerIgnore]
        protected bool HasInitialized { get; private set; }
        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> has loaded it's content.
        /// </summary>
        [ContentSerializerIgnore]
        protected internal bool HasLoadedContent { get; private set; }
        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Entity"/> needs to perform a heavy update.
        /// </summary>
        /// <remarks>Once a heavy update starts, this value is automatically set to false.</remarks>
        [ContentSerializerIgnore]
        protected bool NeedsHeavyUpdate { get; set; }

        bool _isEnabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Entity"/> is updated.
        /// </summary>
        /// <remarks>The default is true with a <see cref="bool"/> value of true.</remarks>
        [ContentSerializer]
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnIsEnabledChanged();
                }
            }
        }

        bool _isVisible = true;
        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Entity"/> is drawn.
        /// </summary>
        /// <remarks>The default is true with a <see cref="bool"/> value of true.</remarks>
        [ContentSerializer]
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnIsVisibleChanged();
                }
            }
        }

        float _depth = 0.0f;
        /// <summary>
        /// Gets or sets the depth (update and draw order) of the <see cref="Entity"/> in the <see cref="ParentCollection"/>.
        /// </summary>
        /// <remarks>The default is zero with a <see cref="float"/> value of 0.0f.</remarks>
        [ContentSerializer]
        public float Depth
        {
            get { return _depth; }
            set
            {
                if (_depth != value)
                {
                    _depth = value;
                    if (ParentCollection != null)
                        ParentCollection.NeedsToSort = true;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public Entity()
            : base()
        {
            IsVisible = true;
            IsEnabled = true;
            NeedsHeavyUpdate = true;
        }

        /// <summary>
        /// Finalizes this instance.
        /// </summary>
        ~Entity()
        {      
            Dispose(false);
        }
        
        /// <summary>
        /// Disposes the <see cref="Entity"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
  
        /// <summary>
        /// Disposes any resources the <see cref="Entity"/> is using.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Recycles the <see cref="Entity"/> so it may be re-used.
        /// </summary>
        public virtual void Recycle()
        {
            if (ParentCollection != null)
                ParentCollection.UnsafeRemove(this);
            ParentCollection = null;
            HasInitialized = false;
            HasLoadedContent = false;
            Depth = 0.0f;
            IsEnabled = true;
            IsVisible = true;
            _flaggedForRemoval = false;
        }

#if WINDOWS
        /// <summary>
        /// Sets a field's value by it's name.
        /// </summary>
        /// <param name="name">Value name.</param>
        /// <param name="value">Value.</param>
        /// <returns>A value indicating whether the field exists and that it's value was sucessfully set.</returns>
        protected internal virtual bool SetField(string name, string value)
        {
            switch (name)
            {
                case "visible":
                case "isvisible":
                    IsVisible = bool.Parse(value);
                    return true;
                case "enabled":
                case "isenabled":
                    IsEnabled = bool.Parse(value);
                    return true;
                case "depth":
                case "layer":
                case "order":
                case "draworder":
                case "updateorder":
                    Depth = float.Parse(value, CultureInfo.InvariantCulture);
                    return true;
             }
            return false;
        }
        bool IImportable.SetField(string name, string value)
        {
            return SetField(name, value);
        }
#endif

        internal void InternalInitliaze()
        {
            if (!HasInitialized)
            {
                Initialize();
                HasInitialized = true;
            }
        }

        /// <summary>
        /// Initializes the <see cref="Entity"/>.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        internal void InternalLoadContent()
        {
            if (!HasLoadedContent)
            {
                LoadContent();
                HasLoadedContent = true;
            }
        }

        /// <summary>
        /// Loads content handled by the <see cref="Entity"/>.
        /// </summary>
        protected virtual void LoadContent()
        {
        }

        void IEntity.LoadContent()
        {
            InternalLoadContent();
        }

        /// <summary>
        /// Completely updates the <see cref="Entity"/>.
        /// </summary>
        /// <param name="time">time</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void InternalUpdate(DeltaGameTime time)
        {
            if (_flaggedForRemoval)
                RemoveImmediately();
            InternalInitliaze();
            InternalLoadContent();
            if (CanUpdate())
            {
                OnBeginUpdate(time);
                LightUpdate(time);
                if (NeedsHeavyUpdate)
                {
                    NeedsHeavyUpdate = false;
                    HeavyUpdate(time);
                }
                OnEndUpdate(time);
            }
        }
        void IEntity.Update(DeltaGameTime time)
        {
            InternalUpdate(time);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> is allowed to update.
        /// </summary>
        /// <returns>A value indicating whether the <see cref="Entity"/> is allowed to update.</returns>
        protected virtual bool CanUpdate()
        {
            return IsEnabled;
        }

        /// <summary>
        /// Updates the <see cref="Entity"/>. Override this method to add custom update logic which is executed every frame.
        /// </summary>
        /// <param name="time">time</param>
        protected virtual void LightUpdate(DeltaGameTime time)
        {
        }  

        /// <summary>
        /// Updates the <see cref="Entity"/>. Override this method to add custom update logic which is too expensive to execute every frame.
        /// </summary>
        /// <param name="time">time</param>
        protected internal virtual void HeavyUpdate(DeltaGameTime time)
        {
        }

        /// <summary>
        /// Completely draws the <see cref="Entity"/>.
        /// </summary>
        /// <param name="time">time</param>
        /// <param name="spriteBatch">spriteBatch</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void InternalDraw(DeltaGameTime time, SpriteBatch spriteBatch)
        {
            if (CanDraw())
            {
                OnBeginDraw(time, spriteBatch);
                Draw(time, spriteBatch);
                OnEndDraw(time, spriteBatch);
            }
        }
        void IEntity.Draw(DeltaGameTime time, SpriteBatch spriteBatch)
        {
            InternalDraw(time, spriteBatch);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> is allowed to draw.
        /// </summary>
        /// <returns>A value indicating whether the <see cref="Entity"/> is allowed to draw.</returns>
        protected virtual bool CanDraw()
        {
            return IsVisible; 
        }

        /// <summary>
        /// Draws the <see cref="Entity"/>. Override to add custom draw logic which is executed every frame.
        /// </summary>
        /// <param name="time">time</param>
        /// <param name="spriteBatch">spriteBatch</param>
        protected virtual void Draw(DeltaGameTime time, SpriteBatch spriteBatch)
        {
        }

        /// <summary>
        /// Called when <see cref="IsEnabled"/> changes.
        /// </summary>
        protected internal virtual void OnIsEnabledChanged()
        {
        } 

        /// <summary>
        /// Called when <see cref="IsVisible"/> changes.
        /// </summary>
        protected internal virtual void OnIsVisibleChanged()
        {
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> is starting to update.
        /// </summary>
        /// <param name="time">time</param>
        protected virtual void OnBeginUpdate(DeltaGameTime time)
        {
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> is finished updating.
        /// </summary>
        /// <param name="time">time</param>
        protected virtual void OnEndUpdate(DeltaGameTime time)
        {
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> is starting to draw.
        /// </summary>
        /// <param name="time">time</param>
        /// <param name="spriteBatch">spriteBatch</param>
        protected virtual void OnBeginDraw(DeltaGameTime time, SpriteBatch spriteBatch)
        {
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> is finished drawing.
        /// </summary>
        /// <param name="time">time</param>
        /// <param name="spriteBatch">spriteBatch</param>
        protected virtual void OnEndDraw(DeltaGameTime time, SpriteBatch spriteBatch)
        {
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> has been added to an <see cref="IEntityCollection"/>.
        /// </summary>
        protected internal virtual void OnAdded()
        {
        }
        void IEntity.OnAdded()
        {
            OnAdded();
        }

        /// <summary>
        /// Called when the <see cref="Entity"/> has been removed from an <see cref="IEntityCollection"/>.
        /// </summary>
        protected internal virtual void OnRemoved()
        {
            _flaggedForRemoval = false;
            ParentCollection = null;
        }
        void IEntity.OnRemoved()
        {
            OnRemoved();
        }

        /// <summary>
        /// Flags the <see cref="Entity"/> to be removed from the <see cref="ParentCollection"/> upon the start of the next frame.
        /// </summary>
        public void RemoveNextFrame()
        {
            _flaggedForRemoval = true;
        }

        /// <summary>
        /// Removes the <see cref="Entity"/> immediately from the <see cref="ParentCollection"/>.
        /// </summary>
        public void RemoveImmediately() //Hopefully this sounds intimidating lol.
        {
            if (ParentCollection != null)
                ParentCollection.UnsafeRemove(this);
        }
    }

}
