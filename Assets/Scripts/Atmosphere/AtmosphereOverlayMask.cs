using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Reserve stencil bit 7 for the atmosphere overlay only. Does not change sprite, color, layout or interaction.
    /// Normal foreground graphics CLEAR the bit; phase-authored graphics SET it. Thus overlapping UI remains correctly layered.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Graphic))]
    public class AtmosphereOverlayMask : MonoBehaviour, IMaterialModifier
    {
        public const int ProtectionBit=128;
        [SerializeField] AtmosphereSpriteBinding binding;
        [Tooltip("Only for the four GPU wallpaper layers whose source material UVs animate without Canvas rebuilds.")]
        [SerializeField] bool syncAnimatedMaterial;
        Graphic graphic;
        struct Key : IEquatable<Key>
        {
            public Material source; public bool protect;
            public bool Equals(Key other)=>source==other.source && protect==other.protect;
            public override bool Equals(object other)=>other is Key key && Equals(key);
            public override int GetHashCode()=>(ReferenceEquals(source,null) ? 0 : source.GetHashCode())*31+(protect ? 1 : 0);
        }
        class Entry { public Material material; public int users; }
        static readonly Dictionary<Key,Entry> cache=new Dictionary<Key,Entry>();
        Key key;
        Entry owned;
        public void Configure(AtmosphereSpriteBinding value) { binding=value; Refresh(); }
        void OnEnable() { graphic=GetComponent<Graphic>(); if (syncAnimatedMaterial) Canvas.willRenderCanvases+=SyncAnimated; Refresh(); }
        void OnDisable() { Canvas.willRenderCanvases-=SyncAnimated; Release(); Refresh(); }
        void OnDestroy() { Release(); }
        public void Refresh() { if (graphic==null) graphic=GetComponent<Graphic>(); if (graphic!=null) graphic.SetMaterialDirty(); }
        public Material GetModifiedMaterial(Material source)
        {
            if (!isActiveAndEnabled || source==null || !source.HasProperty("_Stencil")) return source;
            var wanted=new Key{source=source,protect=binding!=null && binding.DedicatedSpriteActive};
            if (owned==null || !key.Equals(wanted))
            {
                Release(); key=wanted;
                if (!cache.TryGetValue(key,out owned))
                {
                    owned=new Entry{material=new Material(source){name=source.name+" [Atmosphere coverage]",hideFlags=HideFlags.HideAndDontSave}};
                    cache.Add(key,owned);
                }
                owned.users++;
            }
            UpdateProperties(source);
            return owned.material;
        }
        void SyncAnimated() { if (owned!=null && key.source!=null) UpdateProperties(key.source); }
        void UpdateProperties(Material source)
        {
            var material=owned.material;
            material.shader=source.shader;
            material.CopyPropertiesFromMaterial(source);
            int originalRef=source.GetInt("_Stencil"), originalWrite=source.GetInt("_StencilWriteMask");
            bool writes=source.GetInt("_StencilOp")!=(int)StencilOp.Keep;
            material.SetInt("_Stencil",(originalRef & 127)|(key.protect ? ProtectionBit : 0));
            material.SetInt("_StencilReadMask",source.GetInt("_StencilReadMask") & 127);
            material.SetInt("_StencilWriteMask",ProtectionBit|(writes ? originalWrite & 127 : 0));
            material.SetInt("_StencilOp",(int)StencilOp.Replace);
            material.EnableKeyword("UNITY_UI_ALPHACLIP");
            if (material.HasProperty("_UseUIAlphaClip")) material.SetFloat("_UseUIAlphaClip",1);
        }
        void Release()
        {
            if (owned==null) return;
            if (--owned.users==0) { cache.Remove(key); if (Application.isPlaying) Destroy(owned.material); else DestroyImmediate(owned.material); }
            owned=null;
        }
    }
}
