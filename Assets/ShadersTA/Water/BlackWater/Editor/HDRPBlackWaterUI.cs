using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;

public class HDRPBlackWaterUI : ShaderGUI
{
    private bool initialized;
    #region MaterialPropery
    private MaterialProperty _WorldSpaceUV;
    private MaterialProperty _AnimationParams;
    private MaterialProperty _RiverMode;

    private MaterialProperty _ShadingMode;
    private MaterialProperty _EnableCartoonWave;
    private MaterialProperty _WaveLightStep;
    private MaterialProperty _WaveLightFeather;
    private MaterialProperty _EnableCartoonNormal;
    private MaterialProperty _Receiveshadows;
    private MaterialProperty _ShadowStrength;
    private MaterialProperty _TranslucencyOn;
    private MaterialProperty _TranslucencyParams;

    private MaterialProperty _BaseColor;
    private MaterialProperty _ShallowColor;
    private MaterialProperty _TotalAlphaStrength;
    private MaterialProperty _VertexColorMask;
    private MaterialProperty _DepthVertical;
    private MaterialProperty _DepthHorizontal;
    private MaterialProperty _DepthExp;
    private MaterialProperty _EdgeFade;
    private MaterialProperty _HorizonColor;
    private MaterialProperty _VerticalDepthAlpha;
    private MaterialProperty _HorizonDistance;
    private MaterialProperty _WaveTint;

    private MaterialProperty _NormalMapOn;
    private MaterialProperty _BumpMap;
    private MaterialProperty _NormalTiling;
    private MaterialProperty _NormalStrength;
    private MaterialProperty _NormalSpeed;
    private MaterialProperty _EnableDistanceNormals;
    private MaterialProperty _DistanceNormalParams;
    private MaterialProperty _BumpMapLarge;

    private MaterialProperty _IntersectionStyle;
    private MaterialProperty _IntersectionSource;
    private MaterialProperty _IntersectionNoise;
    private MaterialProperty _IntersectionColor;
    private MaterialProperty _IntersectionLength;
    private MaterialProperty _IntersectionClipping;
    private MaterialProperty _IntersectionFalloff;
    private MaterialProperty _IntersectionTiling;
    private MaterialProperty _IntersectionSpeed;
    private MaterialProperty _IntersectionRippleDist;
    private MaterialProperty _IntersectionRippleStrength;

    private MaterialProperty _LightRefractionOn;
    private MaterialProperty _SunReflectionStrength;
    private MaterialProperty _SunReflectionSize;
    private MaterialProperty _SunReflectionDistortion;
    private MaterialProperty _PointSpotLightReflectionExp;
    private MaterialProperty _PointSpotLightStrength;
    private MaterialProperty _EnvRefractionOn;
    private MaterialProperty _ReflectionStrength;
    private MaterialProperty _ReflectionLighting;
    private MaterialProperty _ReflectionFresnel;
    private MaterialProperty _ReflectionDistortion;
    private MaterialProperty _ReflectionBlur;

    private MaterialProperty _WavesOn;
    private MaterialProperty _WaveSpeed;
    private MaterialProperty _WaveHeight;
    private MaterialProperty _WaveCount;
    private MaterialProperty _WaveDirection;
    private MaterialProperty _WaveDistance;
    private MaterialProperty _WaveSteepness;
    private MaterialProperty _WaveNormalStr;
    private MaterialProperty _WaveFadeDistance;

    private MaterialProperty _StencilRef;
    #endregion

    private Section generalSection;
    private Section lightingSection;
    private Section colorSection;
    private Section normalsSection;
    private Section intersectionSection;
    private Section reflectionSection;
    private Section wavesSection;
    private Section advancedSection;

    private void OnEnable(MaterialEditor materialEditor)
    {
        generalSection = new Section(materialEditor, "GENRALSECTION", new GUIContent("General"));
        lightingSection = new Section(materialEditor, "LIGHTINGSECTION", new GUIContent("Lighting"));
        colorSection = new Section(materialEditor, "COLORSECTION", new GUIContent("Color"));
        normalsSection = new Section(materialEditor, "NORMALS", new GUIContent("Normals", "Normal maps represent the small-scale curvature of the water surface. This is used for lighting and reflections"));
        intersectionSection = new Section(materialEditor, "INTERSECTION", new GUIContent("Intersection Foam", "Draws a foam effects on opaque objects that are touching the water"));
        reflectionSection = new Section(materialEditor, "REFLECTIONS", new GUIContent("Reflections", "Sun specular reflection, and environment reflections (reflection probes and planar reflections)"));
        wavesSection = new Section(materialEditor, "WAVES", new GUIContent("Waves", "Parametric gerstner waves, which modify the surface curvature and animate the mesh's vertices"));
        advancedSection = new Section(materialEditor, "ADVANCED", new GUIContent("Advanced"));

        initialized = true;
    }

    public void FindProperties(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        var targetMat = materialEditor?.target as Material;
        if (targetMat == null)
            return;

        _WorldSpaceUV = FindProperty("_WorldSpaceUV", props);
        _AnimationParams = FindProperty("_AnimationParams", props);
        _RiverMode = FindProperty("_RiverMode", props);

        _ShadingMode = FindProperty("_ShadingMode", props);
        _EnableCartoonWave = FindProperty("_EnableCartoonWave", props);
        _WaveLightStep = FindProperty("_WaveLightStep", props);
        _WaveLightFeather = FindProperty("_WaveLightFeather", props);
        _EnableCartoonNormal = FindProperty("_EnableCartoonNormal", props);
        _Receiveshadows = FindProperty("_Receiveshadows", props);
        _ShadowStrength = FindProperty("_ShadowStrength", props);
        _TranslucencyOn = FindProperty("_TranslucencyOn", props);
        _TranslucencyParams = FindProperty("_TranslucencyParams", props);

        _BaseColor = FindProperty("_BaseColor", props);
        _ShallowColor = FindProperty("_ShallowColor", props);
        _TotalAlphaStrength = FindProperty("_TotalAlphaStrength", props);
        _VertexColorMask = FindProperty("_VertexColorMask", props);
        _DepthVertical = FindProperty("_DepthVertical", props);
        _DepthHorizontal = FindProperty("_DepthHorizontal", props);
        _DepthExp = FindProperty("_DepthExp", props);
        _EdgeFade = FindProperty("_EdgeFade", props);
        _HorizonColor = FindProperty("_HorizonColor", props);
        _VerticalDepthAlpha = FindProperty("_VerticalDepthAlpha", props);
        _HorizonDistance = FindProperty("_HorizonDistance", props);
        _WaveTint = FindProperty("_WaveTint", props);

        _NormalMapOn = FindProperty("_NormalMapOn", props);
        _BumpMap = FindProperty("_BumpMap", props);
        _NormalTiling = FindProperty("_NormalTiling", props);
        _NormalStrength = FindProperty("_NormalStrength", props);
        _NormalSpeed = FindProperty("_NormalSpeed", props);
        _EnableDistanceNormals = FindProperty("_EnableDistanceNormals", props);
        _DistanceNormalParams = FindProperty("_DistanceNormalParams", props);
        _BumpMapLarge = FindProperty("_BumpMapLarge", props);

        _IntersectionStyle = FindProperty("_IntersectionStyle", props);
        _IntersectionSource = FindProperty("_IntersectionSource", props);
        _IntersectionNoise = FindProperty("_IntersectionNoise", props);
        _IntersectionColor = FindProperty("_IntersectionColor", props);
        _IntersectionLength = FindProperty("_IntersectionLength", props);
        _IntersectionClipping = FindProperty("_IntersectionClipping", props);
        _IntersectionFalloff = FindProperty("_IntersectionFalloff", props);
        _IntersectionTiling = FindProperty("_IntersectionTiling", props);
        _IntersectionSpeed = FindProperty("_IntersectionSpeed", props);
        _IntersectionRippleDist = FindProperty("_IntersectionRippleDist", props);
        _IntersectionRippleStrength = FindProperty("_IntersectionRippleStrength", props);

        _LightRefractionOn = FindProperty("_LightRefractionOn", props);
        _SunReflectionStrength = FindProperty("_SunReflectionStrength", props);
        _SunReflectionSize = FindProperty("_SunReflectionSize", props);
        _SunReflectionDistortion = FindProperty("_SunReflectionDistortion", props);
        _PointSpotLightReflectionExp = FindProperty("_PointSpotLightReflectionExp", props);
        _PointSpotLightStrength = FindProperty("_PointSpotLightStrength", props);
        _EnvRefractionOn = FindProperty("_EnvRefractionOn", props);
        _ReflectionStrength = FindProperty("_ReflectionStrength", props);
        _ReflectionLighting = FindProperty("_ReflectionLighting", props);
        _ReflectionFresnel = FindProperty("_ReflectionFresnel", props);
        _ReflectionDistortion = FindProperty("_ReflectionDistortion", props);
        _ReflectionBlur = FindProperty("_ReflectionBlur", props);

        _WavesOn = FindProperty("_WavesOn", props);
        _WaveSpeed = FindProperty("_WaveSpeed", props);
        _WaveHeight = FindProperty("_WaveHeight", props);
        _WaveCount = FindProperty("_WaveCount", props);
        _WaveDirection = FindProperty("_WaveDirection", props);
        _WaveDistance = FindProperty("_WaveDistance", props);
        _WaveSteepness = FindProperty("_WaveSteepness", props);
        _WaveNormalStr = FindProperty("_WaveNormalStr", props);
        _WaveFadeDistance = FindProperty("_WaveFadeDistance", props);

        _StencilRef = FindProperty("_StencilRef", props);

    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // base.OnGUI(materialEditor, properties);

        Material targetMat = materialEditor.target as Material;
        if (!initialized) OnEnable(materialEditor);
        //Requires refetching for undo/redo to function
        FindProperties(materialEditor, properties);

        EditorGUI.BeginChangeCheck();
        DrawGeneral(materialEditor);
        DrawLighting(materialEditor);
        DrawColor(materialEditor);
        DrawNormal(materialEditor);
        DrawIntersection(materialEditor);
        DrawReflections(materialEditor);
        DrawWaves(materialEditor);
        DrawAdvanced(materialEditor);
        //----------------------------
        if (EditorGUI.EndChangeCheck())
        {
            ApplyChanges(targetMat);
        }
    }

    #region Sections
    private void DrawGeneral(MaterialEditor materialEditor)
    {
        generalSection.DrawHeader(() => SwitchSection(generalSection));
        if (EditorGUILayout.BeginFadeGroup(generalSection.anim.faded))
        {
            EditorGUILayout.Space();

            var worldSpaceUV = (int)_WorldSpaceUV.floatValue;
            worldSpaceUV = EditorGUILayout.Popup(new GUIContent("UV Source", "选着UV方式"), worldSpaceUV, new string[] { "Mesh UV", "World XZ projected" });
            _WorldSpaceUV.floatValue = worldSpaceUV;

            Vector2 tempAnimDir = new Vector2(_AnimationParams.vectorValue.x, _AnimationParams.vectorValue.y);
            float tempAnimSpeed = _AnimationParams.vectorValue.z;
            tempAnimDir = EditorGUILayout.Vector2Field("Direction", tempAnimDir);
            tempAnimSpeed = EditorGUILayout.FloatField("Speed", tempAnimSpeed);
            _AnimationParams.vectorValue = new Vector4(tempAnimDir.x, tempAnimDir.y, tempAnimSpeed, 0);

            bool riverMode = (int)_RiverMode.floatValue == 1;
            riverMode = EditorGUILayout.Toggle("River Mode", riverMode);
            _RiverMode.floatValue = riverMode ? 1 : 0;
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawLighting(MaterialEditor materialEditor)
    {
        lightingSection.DrawHeader(() => SwitchSection(lightingSection));

        if (EditorGUILayout.BeginFadeGroup(lightingSection.anim.faded))
        {
            EditorGUILayout.Space();
            var enableCarToonWave = _EnableCartoonWave.floatValue == 1;
            enableCarToonWave = EditorGUILayout.Toggle("Enable Cartoon Wave", enableCarToonWave);
            _EnableCartoonWave.floatValue = enableCarToonWave ? 1 : 0;

            if (enableCarToonWave)
            {
                materialEditor.ShaderProperty(_WaveLightStep, _WaveLightStep.displayName);
                materialEditor.ShaderProperty(_WaveLightFeather, _WaveLightFeather.displayName);
            }

            var enableCarToonNormal = _EnableCartoonNormal.floatValue == 1;
            enableCarToonNormal = EditorGUILayout.Toggle("Enable Cartoon Normal", enableCarToonWave);
            _EnableCartoonNormal.floatValue = enableCarToonNormal ? 1 : 0;

            var enableReceiveshadows = _Receiveshadows.floatValue == 1;
            enableReceiveshadows = EditorGUILayout.Toggle("Enable Receive Shadows", enableReceiveshadows);
            _Receiveshadows.floatValue = enableReceiveshadows ? 1 : 0;
            if (enableReceiveshadows)
                materialEditor.ShaderProperty(_ShadowStrength, _ShadowStrength.displayName);

            var enableTranslucency = _TranslucencyOn.floatValue == 1;
            enableTranslucency = EditorGUILayout.Toggle("Enable Translucency", enableTranslucency);
            _TranslucencyOn.floatValue = enableTranslucency ? 1 : 0;
            if (enableTranslucency)
            {
                float translucencyStr = EditorGUILayout.Slider("Strength", _TranslucencyParams.vectorValue.x, 0f, 3f);
                float translucencyExp = EditorGUILayout.Slider(new GUIContent("Exponent", "Essentially controls the width/scale of the effect"), _TranslucencyParams.vectorValue.y, 1f, 16f);
                float translucencyMask = EditorGUILayout.Slider(new GUIContent("Curvature mask", "Masks the effect by the orientation of the surface. Surfaces facing away from the sun will receive less of an effect"), _TranslucencyParams.vectorValue.z, 0f, 1f);
                float translucencyReflectionMask = EditorGUILayout.Slider(new GUIContent("Reflection Mask", "Controls how strongly reflections are laid over the effect. A value of 1 is physically accurate"), _TranslucencyParams.vectorValue.w, 0, 1);

                _TranslucencyParams.vectorValue = new Vector4(translucencyStr, translucencyExp, translucencyMask, translucencyReflectionMask);
            }
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawColor(MaterialEditor materialEditor)
    {
        colorSection.DrawHeader(() => SwitchSection(colorSection));

        if (EditorGUILayout.BeginFadeGroup(colorSection.anim.faded))
        {
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
            materialEditor.ShaderProperty(_ShallowColor, _ShallowColor.displayName);
            materialEditor.ShaderProperty(_TotalAlphaStrength, _TotalAlphaStrength.displayName);
            materialEditor.ShaderProperty(_VertexColorMask, _VertexColorMask.displayName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fog/Density", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(_DepthVertical, _DepthVertical.displayName);
            materialEditor.ShaderProperty(_DepthHorizontal, _DepthHorizontal.displayName);

            EditorGUI.indentLevel++;
            var enableExponential = _DepthExp.floatValue == 1;
            enableExponential = EditorGUILayout.Toggle("Enable Exponential", enableExponential);
            _DepthExp.floatValue = enableExponential ? 1 : 0;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            materialEditor.ShaderProperty(_EdgeFade, _EdgeFade.displayName);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(_HorizonColor, _HorizonColor.displayName);
            materialEditor.ShaderProperty(_VerticalDepthAlpha, _VerticalDepthAlpha.displayName);
            materialEditor.ShaderProperty(_HorizonDistance, _HorizonDistance.displayName);
            materialEditor.ShaderProperty(_WaveTint, _WaveTint.displayName);
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawNormal(MaterialEditor materialEditor)
    {
        normalsSection.DrawHeader(() => SwitchSection(normalsSection));

        if (EditorGUILayout.BeginFadeGroup(normalsSection.anim.faded))
        {
            EditorGUILayout.Space();
            var enableNormal = _NormalMapOn.floatValue == 1;
            enableNormal = EditorGUILayout.Toggle("Enable Normal", enableNormal);
            _NormalMapOn.floatValue = enableNormal ? 1 : 0;

            if (enableNormal)
            {
                materialEditor.TextureProperty(_BumpMap, "Normal map");
                _NormalTiling.floatValue = DrawFloatTicker(_NormalTiling.floatValue, _NormalTiling.displayName);
                _NormalSpeed.floatValue = DrawFloatTicker(_NormalSpeed.floatValue, _NormalSpeed.displayName);
                materialEditor.ShaderProperty(_NormalStrength, _NormalStrength.displayName);

                var enableDistanceNormal = _EnableDistanceNormals.floatValue == 1;
                enableDistanceNormal = EditorGUILayout.Toggle("Enable Distance Normal", enableDistanceNormal);
                _EnableDistanceNormals.floatValue = enableDistanceNormal ? 1 : 0;
                if (enableDistanceNormal)
                {
                    materialEditor.TextureProperty(_BumpMapLarge, "Normal map");
                    DrawRangeSlider(_DistanceNormalParams, 0f, 500, "Blend distance range", tooltip: "Min/max distance the effect should start to blend in");
                    float distanceTilingMultiplier = _DistanceNormalParams.vectorValue.z;
                    distanceTilingMultiplier = EditorGUILayout.Slider("Tiling multiplier", distanceTilingMultiplier, 0, 1);
                    _DistanceNormalParams.vectorValue = new Vector4(_DistanceNormalParams.vectorValue.x, _DistanceNormalParams.vectorValue.y, distanceTilingMultiplier, _DistanceNormalParams.vectorValue.w);
                }
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawIntersection(MaterialEditor materialEditor)
    {
        intersectionSection.DrawHeader(() => SwitchSection(intersectionSection));

        if (EditorGUILayout.BeginFadeGroup(intersectionSection.anim.faded))
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Style", GUILayout.Width(EditorGUIUtility.labelWidth));
                _IntersectionStyle.floatValue = GUILayout.Toolbar((int)_IntersectionStyle.floatValue,
                    new GUIContent[] { new GUIContent("None"), new GUIContent("Sharp"), new GUIContent("Smooth"), }, GUILayout.MaxWidth((250f))
                );
            }

            if (_IntersectionStyle.floatValue > 0)
            {
                materialEditor.ShaderProperty(_IntersectionSource, new GUIContent("Gradient source", null, "The effect requires a grayscale gradient to work with, this sets what information should be used for this"));
                materialEditor.TextureProperty(_IntersectionNoise, "Texture (R=Mask)");
                materialEditor.ShaderProperty(_IntersectionColor, _IntersectionColor.displayName);
                materialEditor.ShaderProperty(_IntersectionLength, _IntersectionLength.displayName);
                materialEditor.ShaderProperty(_IntersectionFalloff, _IntersectionFalloff.displayName);
                _IntersectionTiling.floatValue = DrawFloatTicker(_IntersectionTiling.floatValue);
                _IntersectionSpeed.floatValue = DrawFloatTicker(_IntersectionSpeed.floatValue);

                if (_IntersectionStyle.floatValue == 1f)
                {
                    materialEditor.ShaderProperty(_IntersectionClipping, _IntersectionClipping.displayName);
                    _IntersectionRippleDist.floatValue = DrawFloatTicker(_IntersectionRippleDist.floatValue);
                    materialEditor.ShaderProperty(_IntersectionRippleStrength, _IntersectionRippleStrength.displayName);
                }

                EditorGUILayout.Space();
            }
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawReflections(MaterialEditor materialEditor)
    {
        reflectionSection.DrawHeader(() => SwitchSection(reflectionSection));

        if (EditorGUILayout.BeginFadeGroup(reflectionSection.anim.faded))
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Light reflections", EditorStyles.boldLabel);
            var enableLightRef = _LightRefractionOn.floatValue == 1;
            enableLightRef = EditorGUILayout.Toggle("Enable Light Refraction", enableLightRef);
            _LightRefractionOn.floatValue = enableLightRef ? 1 : 0;

            EditorGUILayout.Space();
            if (enableLightRef)
            {
                materialEditor.ShaderProperty(_SunReflectionStrength, _SunReflectionStrength.displayName);
                materialEditor.ShaderProperty(_SunReflectionSize, _SunReflectionSize.displayName);
                materialEditor.ShaderProperty(_SunReflectionDistortion, _SunReflectionDistortion.displayName);
                materialEditor.ShaderProperty(_PointSpotLightReflectionExp, _PointSpotLightReflectionExp.displayName);
                materialEditor.ShaderProperty(_PointSpotLightStrength, _PointSpotLightStrength.displayName);
            }

            var enableEnvRef = _EnvRefractionOn.floatValue == 1;
            enableEnvRef = EditorGUILayout.Toggle("Enable Env Refraction", enableEnvRef);
            _EnvRefractionOn.floatValue = enableEnvRef ? 1 : 0;
            if (enableEnvRef)
            {
                materialEditor.ShaderProperty(_ReflectionStrength, _ReflectionStrength.displayName);
                materialEditor.ShaderProperty(_ReflectionLighting, _ReflectionLighting.displayName);
                materialEditor.ShaderProperty(_ReflectionFresnel, new GUIContent(_ReflectionFresnel.displayName, "Masks the reflection by the viewing angle in relationship to the surface (including wave curvature), which is more true to nature (known as fresnel)"));
                materialEditor.ShaderProperty(_ReflectionDistortion, new GUIContent(_ReflectionDistortion.displayName, "Distorts the reflection by the wave normals and normal map"));
                materialEditor.ShaderProperty(_ReflectionBlur, new GUIContent(_ReflectionBlur.displayName, "Blurs the reflection probe, this can be used for a more general reflection of colors"));
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawWaves(MaterialEditor materialEditor)
    {
        wavesSection.DrawHeader(() => SwitchSection(wavesSection));

        if (EditorGUILayout.BeginFadeGroup(wavesSection.anim.faded))
        {
            EditorGUILayout.Space();
            var enableWave = _WavesOn.floatValue == 1;
            enableWave = EditorGUILayout.Toggle("Enable Wave", enableWave);
            _WavesOn.floatValue = enableWave ? 1 : 0;
            if (enableWave && _RiverMode.floatValue == 0)
            {
                _WaveSpeed.floatValue = DrawFloatTicker(_WaveSpeed.floatValue);
                materialEditor.ShaderProperty(_WaveHeight, _WaveHeight.displayName);
                materialEditor.ShaderProperty(_WaveCount, _WaveCount.displayName);

                Vector4 waveDir = _WaveDirection.vectorValue;
                Vector2 waveDir1;
                Vector2 waveDir2;
                waveDir1.x = waveDir.x;
                waveDir1.y = waveDir.y;
                waveDir2.x = waveDir.z;
                waveDir2.y = waveDir.w;

                EditorGUILayout.LabelField("Direction");
                EditorGUI.indentLevel++;
                waveDir2 = EditorGUILayout.Vector2Field("Sub layer 2 (X)", waveDir2);
                waveDir1 = EditorGUILayout.Vector2Field("Sub layer 1 (Z)", waveDir1);
                EditorGUI.indentLevel--;

                waveDir = new Vector4(waveDir1.x, waveDir1.y, waveDir2.x, waveDir2.y);
                _WaveDirection.vectorValue = waveDir;
                materialEditor.ShaderProperty(_WaveDistance, _WaveDistance.displayName);
                materialEditor.ShaderProperty(_WaveSteepness, _WaveSteepness.displayName);
                materialEditor.ShaderProperty(_WaveNormalStr, _WaveNormalStr.displayName);
                DrawRangeSlider(_WaveFadeDistance, 0f, 500f, tooltip: "Fades out the waves between the start- and end distance. This can avoid tiling artifacts in the distance");
            }

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFadeGroup();
    }
    private void DrawAdvanced(MaterialEditor materialEditor)
    {
        advancedSection.DrawHeader(() => SwitchSection(advancedSection));

        if (EditorGUILayout.BeginFadeGroup(advancedSection.anim.faded))
        {
            EditorGUILayout.Space();

            materialEditor.EnableInstancingField();
            materialEditor.RenderQueueField();

            //materialEditor.DoubleSidedGIField();

            materialEditor.ShaderProperty(_StencilRef, _StencilRef.displayName);

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFadeGroup();
    }
    #endregion

    #region Supports Function
    private void ApplyChanges(Material targetMat)
    {
        CoreUtils.SetKeyword(targetMat, "_RIVER", _RiverMode.floatValue == 1f);
        CoreUtils.SetKeyword(targetMat, "_CARTOONWAVE", _EnableCartoonWave.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_CARTOONNORMAL", _EnableCartoonNormal.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_RECEIVE_SHADOWS_OFF", _Receiveshadows.floatValue == 0);
        CoreUtils.SetKeyword(targetMat, "_TRANSLUCENCY", _TranslucencyOn.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_NORMALMAP", _NormalMapOn.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_DISTANCE_NORMALS", _EnableDistanceNormals.floatValue == 1);
        //CoreUtils.SetKeyword(targetMat, "_ADVANCED_SHADING", _ShadingMode.floatValue == 1f);
        CoreUtils.SetKeyword(targetMat, "_SHARP_INERSECTION", _IntersectionStyle.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_SMOOTH_INTERSECTION", _IntersectionStyle.floatValue == 2);
        CoreUtils.SetKeyword(targetMat, "_SPECULARHIGHLIGHTS_ON", _LightRefractionOn.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_ENVIRONMENTREFLECTIONS_ON", _EnvRefractionOn.floatValue == 1);
        CoreUtils.SetKeyword(targetMat, "_WAVES", _WavesOn.floatValue == 1);
    }
    private void SwitchSection(Section s)
    {
        generalSection.Expanded = (s == generalSection) && !generalSection.Expanded;
        lightingSection.Expanded = (s == lightingSection) && !lightingSection.Expanded;
        colorSection.Expanded = (s == colorSection) && !colorSection.Expanded;
        normalsSection.Expanded = (s == normalsSection) && !normalsSection.Expanded;
        intersectionSection.Expanded = (s == intersectionSection) && !intersectionSection.Expanded;
        reflectionSection.Expanded = (s == reflectionSection) && !reflectionSection.Expanded;
        wavesSection.Expanded = (s == wavesSection) && !wavesSection.Expanded;
        advancedSection.Expanded = (s == advancedSection) && !advancedSection.Expanded;
    }
    //Section toggles
    public class Section
    {
        private const float ANIM_SPEED = 16f;
        public bool Expanded
        {
            get { return SessionState.GetBool(id, false); }
            set { SessionState.SetBool(id, value); }
        }
        public bool showHelp;
        public AnimBool anim;

        public readonly string id;
        public GUIContent title;

        public Section(MaterialEditor owner, string id, GUIContent title)
        {
            this.id = "HDRPBLACKWATER" + id + "_SECTION";
            this.title = title;

            anim = new AnimBool(true);
            anim.valueChanged.AddListener(owner.Repaint);
            anim.speed = ANIM_SPEED;
            anim.target = Expanded;
        }

        public void DrawHeader(Action clickAction)
        {
            DrawHeader(title, Expanded, clickAction);
            anim.target = Expanded;
        }

        private const float HeaderHeight = 25f;
        private bool DrawHeader(GUIContent content, bool isExpanded, Action clickAction = null)
        {
            Rect backgroundRect = GUILayoutUtility.GetRect(1f, HeaderHeight);

            var labelRect = backgroundRect;
            labelRect.xMin += 8f;
            labelRect.xMax -= 20f + 16 + 5;

            var foldoutRect = backgroundRect;
            foldoutRect.xMin -= 8f;
            foldoutRect.y += 0f;
            foldoutRect.width = HeaderHeight;
            foldoutRect.height = HeaderHeight;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            if (backgroundRect.Contains(Event.current.mousePosition)) backgroundTint *= EditorGUIUtility.isProSkin ? 1.5f : 0.9f;

            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);

            // Foldout
            GUI.Label(foldoutRect, new GUIContent(isExpanded ? "O" : "≡"), EditorStyles.boldLabel);

            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        isExpanded = !isExpanded;
                        if (clickAction != null) clickAction.Invoke();
                    }

                    e.Use();
                }
            }

            return isExpanded;
        }
    }

    private float DrawFloatTicker(float value, string label = null, string tooltip = null)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(label, null, tooltip));
        if (GUILayout.Button("<<", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(20)))
        {
            value -= 0.1f;
        }
        if (GUILayout.Button("<", EditorStyles.miniButtonMid, GUILayout.MaxWidth(17)))
        {
            value -= 0.01f;
        }

        value = EditorGUILayout.FloatField(value, GUILayout.MaxWidth(45));

        if (GUILayout.Button(">", EditorStyles.miniButtonMid, GUILayout.MaxWidth(17)))
        {
            value += 0.01f;
        }
        if (GUILayout.Button(">>", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20)))
        {
            value += 0.1f;
        }
        EditorGUILayout.EndHorizontal();

        return value;
    }
    private void DrawRangeSlider(MaterialProperty prop, float min, float max, string label = null, string tooltip = null)
    {
        float minVal = prop.vectorValue.x;
        float maxVal = prop.vectorValue.y;

        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(new GUIContent(label ?? prop.displayName, null, tooltip), GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField(System.Math.Round(minVal, 2).ToString(), GUILayout.Width(40f));
            EditorGUILayout.MinMaxSlider(ref minVal, ref maxVal, min, max);
            EditorGUILayout.LabelField(System.Math.Round(maxVal, 2).ToString(), GUILayout.Width(40f));
        }

        if (EditorGUI.EndChangeCheck())
        {
            prop.vectorValue = new Vector4(minVal, maxVal, prop.vectorValue.z, prop.vectorValue.w);
        }
    }
    #endregion
}
