using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEditor;
using InTerra;

public class InTerraObjectShaderGUI : ShaderGUI
{
	bool zeroLayer = true;
	bool redLayer = true;
	bool redLayerBlend = true;
	bool greenLayer = true;
	bool greenLayerBlend = true;
	bool uniqueLayer = true;

	bool terrainInfo = false;
	bool objectInfo = false;
	bool objectInfoInit = false;
	bool minmax1 = false;
	bool minmax2 = false;
	bool minmaxNi = false;
	bool minmaxMip = false;
	bool nIntersect = false;
	bool tessDistances = false;

	MaterialProperty[] properties;
	Vector2 ScrollPos;

	Terrain terrain = null;
	TerrainLayer[] tLayers = null;
	bool isOnTerrain = false;

	List<Renderer> okTerrain = new List<Renderer>();
	List<Renderer> noTerrain = new List<Renderer>();
	List<Renderer> wrongTerrain = new List<Renderer>();

	enum NumberOfLayers
	{
		[Description("One Pass")] OnePass,
		[Description("One Layer")] OneLayer,
		[Description("Two Layers")] TwoLayers
	}

	enum TessellationMode
	{
		[Description("None")] None,
		[Description("Phong")] Phong
	}

	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		this.properties = properties;
		Material targetMat = materialEditor.target as Material;
		Rect textureRect;
		bool disableUpdates = InTerra_Setting.DisableAllAutoUpdates;
		bool updateDict = InTerra_Setting.DictionaryUpdate;
		bool updateAtOpen = InTerra_Setting.ObjectGUICheckAndUpdateAtOpen;

		//-------------------------- FONT STYLES -------------------------------
		var styleButtonBold = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
		var styleBoldLeft = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
		var styleLeft = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
		var styBoldCenter = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
		var styleMiniBold = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
		var styleMiniRight = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
		var styleMini = new GUIStyle(EditorStyles.miniLabel);

		//===================== TERRAIN & OBJECTS DATA =========================
		DictionaryMaterialTerrain materialTerrain = InTerra_Data.GetUpdaterScript().MaterialTerrain;
		Terrain[] terrains = Terrain.activeTerrains;

		if (terrains.Length == 0)
		{
			EditorGUILayout.HelpBox("There is no Terrain in this Scene!", MessageType.Warning);
			GUI.enabled = false;
		}
		else
		{
			if (updateAtOpen)
			{
				if (!objectInfoInit)
				{
					if (!disableUpdates) InTerra_Data.UpdateTerrainData(updateDict);
					if (materialTerrain.ContainsKey(targetMat))
					{
						GetTerrain();
						CreateObjectsLists(targetMat, terrain);
					}
					objectInfoInit = true;
				}
				WrongTerrainWarning(terrain);
			}

			if (materialTerrain.ContainsKey(targetMat))
			{
				if (materialTerrain[targetMat] == null)
				{
					EditorGUILayout.HelpBox("The avarge position of the Objects using this Material is outside of any Terrain!", MessageType.Warning);
					GUI.enabled = false;
				}
				else
				{
					GetTerrain();
				}
			}
			else
			{
				EditorGUILayout.HelpBox("There are no Terrain Data for this Material or this Material is not assigned to any Object in this Scene!", MessageType.Warning);
				GUI.enabled = false;
			}
		}

		//=======================================================================
		//----------------------|   OBJECT TEXTURES    |-------------------------
		//=======================================================================

		materialEditor.ShaderProperty(FindProperty("_USEREDLAYER"), "Use Red Layer");
		materialEditor.ShaderProperty(FindProperty("_USEGREENLAYER"), "Use Green Layer");
		//-------------------------- Zero Layer -------------------------
		EditorGUI.indentLevel = 0;
		zeroLayer = EditorGUILayout.Foldout(zeroLayer, "Zero Layer", true);
		EditorGUI.indentLevel = 1;
		if (zeroLayer)
		{
			materialEditor.ShaderProperty(FindProperty("_ZL_Color"), "ZL - Color");
			materialEditor.ShaderProperty(FindProperty("______ZL_Use_BC_tex"), "ZL - Use BC tex");
			materialEditor.ShaderProperty(FindProperty("______ZL_BC_tex"), "ZL - BC tex");
			materialEditor.ShaderProperty(FindProperty("______ZL_BC_Tint"), "ZL - BC Tint");
			materialEditor.ShaderProperty(FindProperty("______ZL_BC_Tiling"), "ZL - BC Tiling");
			materialEditor.ShaderProperty(FindProperty("______ZL_BC_Rotate"), "ZL - BC Rotate");

			materialEditor.ShaderProperty(FindProperty("_ZL_N_tex"), "ZL - N tex");
			materialEditor.ShaderProperty(FindProperty("______ZL_N_Tiling"), "ZL - N Tiling");
			materialEditor.ShaderProperty(FindProperty("______ZL_N_Rotate"), "ZL - N Rotate");
			materialEditor.ShaderProperty(FindProperty("______ZL_N_intensity"), "ZL - N Intensity");
			materialEditor.ShaderProperty(FindProperty("_ZL_N_Break_Tiling_Frequency"), "ZL - N .Break Tiling. Frequency");
			materialEditor.ShaderProperty(FindProperty("_ZL_N_Brak_Tiling_MaskTex"), "ZL - N.Break Tiling. MaskTex");

			materialEditor.ShaderProperty(FindProperty("_ZL_Add_Cavity"), "ZL - Add Cavity");
			materialEditor.ShaderProperty(FindProperty("______ZL_Cavity_Mask"), "ZL - Cavity Mask");
			materialEditor.ShaderProperty(FindProperty("______ZL_Cavity_Color"), "ZL - Cavity Color");
			materialEditor.ShaderProperty(FindProperty("_ZL_Cavity_Intensity"), "ZL - Cavity Intensity");
			materialEditor.ShaderProperty(FindProperty("_ZL_Roughness"), "ZL - Roughness");
			materialEditor.ShaderProperty(FindProperty("_ZL_Metallic"), "ZL - Metallic");

			materialEditor.ShaderProperty(FindProperty("______ZL_Use_Mask_tex"), "ZL - Use Mask tex");
			materialEditor.ShaderProperty(FindProperty("______ZL_Mask_tex"), "ZL - Mask tex");
			materialEditor.ShaderProperty(FindProperty("______ZL_Mask_TilingOffset"), "ZL - Mask TilingOffset");
			materialEditor.ShaderProperty(FindProperty("______ZL_Mask_Rotate"), "ZL - Mask Rotate");
			materialEditor.ShaderProperty(FindProperty("_ZL_BC_Saturation"), "ZL - BC Saturation");

			EditorGUI.indentLevel = 0;
		}

		//-------------------------- Red Layer -------------------------
		EditorGUI.indentLevel = 0;
		redLayer = EditorGUILayout.Foldout(redLayer, "Red Layer", true);
		EditorGUI.indentLevel = 1;
		if (redLayer)
		{
			materialEditor.ShaderProperty(FindProperty("_RL_USE_BC_prop"), "RL - USE BC prop.");
			materialEditor.ShaderProperty(FindProperty("______RL_Color"), "RL - Color");
			materialEditor.ShaderProperty(FindProperty("______RL_Use_BC_tex"), "RL - Use BC tex");
			materialEditor.ShaderProperty(FindProperty("______RL_BC_tex"), "RL - BC tex");
			materialEditor.ShaderProperty(FindProperty("______RL_BC_Tint"), "RL - BC Tint");
			materialEditor.ShaderProperty(FindProperty("______RL_BC_Tiling"), "RL - BC Tiling");
			materialEditor.ShaderProperty(FindProperty("______RL_BC_Rotate"), "RL - BC Rotate");

			materialEditor.ShaderProperty(FindProperty("_RL_USE_N_prop"), "RL - USE N prop.");
			materialEditor.ShaderProperty(FindProperty("______RL_N_tex"), "RL - N tex");
			materialEditor.ShaderProperty(FindProperty("______RL_N_UV_Tiling"), "RL - N UV Tiling");
			materialEditor.ShaderProperty(FindProperty("______RL_N_UV_Rotate"), "RL - N UV Rotate");
			materialEditor.ShaderProperty(FindProperty("______RL_N_intensity"), "RL - N intensity");

			materialEditor.ShaderProperty(FindProperty("_RL_Cavity_Intensity"), "RL - Cavity Intensity");
			materialEditor.ShaderProperty(FindProperty("______RL_Cavity_Color"), "RL - Cavity Color");
			materialEditor.ShaderProperty(FindProperty("______RL_Cavity_Mask"), "RL - Cavity Mask");

			materialEditor.ShaderProperty(FindProperty("_RL_USE_Mask_prop"), "RL - USE Mask prop.");
			materialEditor.ShaderProperty(FindProperty("______RL_Mask_Tex"), "RL - Mask Tex");
			materialEditor.ShaderProperty(FindProperty("______RL_Mask_Tiling"), "RL - Mask Tiling");
			materialEditor.ShaderProperty(FindProperty("______RL_Mask_Rotate"), "RL - Mask Rotate");
			materialEditor.ShaderProperty(FindProperty("_RL_Roughness"), "RL - Roughness");
			materialEditor.ShaderProperty(FindProperty("_RL_Metallic"), "RL - Metallic");

			EditorGUI.indentLevel = 0;
		}

		//-------------------------- Red Layer Blend -------------------------
		EditorGUI.indentLevel = 0;
		redLayerBlend = EditorGUILayout.Foldout(redLayerBlend, "Red Layer Blend", true);
		EditorGUI.indentLevel = 1;
		if (redLayerBlend)
		{
			materialEditor.ShaderProperty(FindProperty("_RLB_BC_Blend_Intensity"), "RLB - BC Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______RLB_Use_BC_Additive_Blend"), "RLB - Use BC Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_RLB_N_Blend_Intensity"), "RLB - N Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______RLB_Use_N_Additive_Blend"), "RLB - Use N Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_RLB_Mask_Blend_Intensity"), "RLB - Mask Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______RLB_Use_Mask_Additive_Blend"), "RLB - Use Mask Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_RLB_Use_Slope_Mask"), "RLB - Use Slope Mask");
			materialEditor.ShaderProperty(FindProperty("______RLB_Slope_Mask_Offset"), "RLB - Slope Mask Offset");
			materialEditor.ShaderProperty(FindProperty("______RLB_Slope_Mask_Contr"), "RLB - Slope Mask Contr");
			materialEditor.ShaderProperty(FindProperty("______RLB_Use_Mask_Tex"), "RLB - Use Mask Tex");
			materialEditor.ShaderProperty(FindProperty("_RLB_Mask_Tex"), "RLB - Mask Tex");
			materialEditor.ShaderProperty(FindProperty("______RLB_Mask_Tex_Spread"), "RLB - Mask Tex Spread");
			materialEditor.ShaderProperty(FindProperty("______RLB_Mask_Tex_Contrast"), "RLB - Mask Tex Contrast");
			materialEditor.ShaderProperty(FindProperty("_RLB_Add_VertexPaint_False_Erase"), "RLB - Add VertexPaint (False = Erase)");
			materialEditor.ShaderProperty(FindProperty("_RLB_VertexColorEdgeNoiseTex"), "RLB - VertexColorEdgeNoiseTex");

			EditorGUI.indentLevel = 0;
		}

		//-------------------------- Green Layer -------------------------
		EditorGUI.indentLevel = 0;
		greenLayer = EditorGUILayout.Foldout(greenLayer, "Green Layer", true);
		EditorGUI.indentLevel = 1;
		if (greenLayer)
		{
			materialEditor.ShaderProperty(FindProperty("_GL_USE_BC_prop"), "GL - USE BC prop.");
			materialEditor.ShaderProperty(FindProperty("______GL_Color"), "GL - Color");
			materialEditor.ShaderProperty(FindProperty("______GL_Use_BC_tex"), "GL - Use BC tex");
			materialEditor.ShaderProperty(FindProperty("______GL_BC_tex"), "GL - BC tex");
			materialEditor.ShaderProperty(FindProperty("______GL_BC_Tint"), "GL - BC Tint");
			materialEditor.ShaderProperty(FindProperty("______GL_BC_Tiling"), "GL - BC Tiling");
			materialEditor.ShaderProperty(FindProperty("______GL_BC_Rotate"), "GL - BC Rotate");

			materialEditor.ShaderProperty(FindProperty("_GL_USE_N_prop"), "GL - USE N prop.");
			materialEditor.ShaderProperty(FindProperty("______GL_N_tex"), "GL - N tex");
			materialEditor.ShaderProperty(FindProperty("______GL_N_UV_Tiling"), "GL - N UV Tiling");
			materialEditor.ShaderProperty(FindProperty("______GL_N_UV_Rotate"), "GL - N UV Rotate");
			materialEditor.ShaderProperty(FindProperty("______GL_N_intensity"), "GL - N intensity");

			materialEditor.ShaderProperty(FindProperty("_GL_Cavity_Intensity"), "GL - Cavity Intensity");
			materialEditor.ShaderProperty(FindProperty("______GL_Cavity_Color"), "GL - Cavity Color");
			materialEditor.ShaderProperty(FindProperty("______GL_Cavity_Mask"), "GL - Cavity Mask");

			materialEditor.ShaderProperty(FindProperty("_GL_USE_Mask_prop"), "GL - USE Mask prop.");
			materialEditor.ShaderProperty(FindProperty("______GL_Mask_Tex"), "GL - Mask Tex");
			materialEditor.ShaderProperty(FindProperty("______GL_Mask_Tiling"), "GL - Mask Tiling");
			materialEditor.ShaderProperty(FindProperty("______GL_Mask_Rotate"), "GL - Mask Rotate");
			materialEditor.ShaderProperty(FindProperty("_GL_Roughness"), "GL - Roughness");
			materialEditor.ShaderProperty(FindProperty("_GL_Metallic"), "GL - Metallic");

			EditorGUI.indentLevel = 0;
		}

		//-------------------------- Green Layer Blend -------------------------
		EditorGUI.indentLevel = 0;
		greenLayerBlend = EditorGUILayout.Foldout(greenLayerBlend, "Green Layer Blend", true);
		EditorGUI.indentLevel = 1;
		if (greenLayerBlend)
		{
			materialEditor.ShaderProperty(FindProperty("_GLB_BC_Blend_Intensity"), "GLB - BC Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______GLB_Use_BC_Additive_Blend"), "GLB - Use BC Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_GLB_N_Blend_Intensity"), "GLB - N Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______GLB_Use_N_Additive_Blend"), "GLB - Use N Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_GLB_Mask_Blend_Intensity"), "GLB - Mask Blend Intensity");
			materialEditor.ShaderProperty(FindProperty("______GLB_Use_Mask_Additive_Blend"), "GLB - Use Mask Additive Blend");
			materialEditor.ShaderProperty(FindProperty("_GLB_Use_Slope_Mask"), "GLB - Use Slope Mask");
			materialEditor.ShaderProperty(FindProperty("______GLB_Slope_Mask_Offset"), "GLB - Slope Mask Offset");
			materialEditor.ShaderProperty(FindProperty("______GLB_Slope_Mask_Contr"), "GLB - Slope Mask Contr");
			materialEditor.ShaderProperty(FindProperty("______GLB_Use_Mask_Tex"), "GLB - Use Mask Tex");
			materialEditor.ShaderProperty(FindProperty("_GLB_Mask_Tex"), "GLB - Mask Tex");
			materialEditor.ShaderProperty(FindProperty("______GLB_Mask_Tex_Spread"), "GLB - Mask Tex Spread");
			materialEditor.ShaderProperty(FindProperty("______GLB_Mask_Tex_Contrast"), "GLB - Mask Tex Contrast");
			materialEditor.ShaderProperty(FindProperty("_GLB_Add_VertexPaint_False_Erase"), "GLB - Add VertexPaint (False = Erase)");
			materialEditor.ShaderProperty(FindProperty("_GLB_VertexColorEdgeNoiseTex"), "GLB - VertexColorEdgeNoiseTex");

			EditorGUI.indentLevel = 0;
		}

		//-------------------------- Green Layer Blend -------------------------
		EditorGUI.indentLevel = 0;
		uniqueLayer = EditorGUILayout.Foldout(uniqueLayer, "Unique Layer", true);
		EditorGUI.indentLevel = 1;
		if (uniqueLayer)
		{
			materialEditor.ShaderProperty(FindProperty("_UL_BC_tex"), "UL - BC tex");
			materialEditor.ShaderProperty(FindProperty("______UL_BC_Intensity"), "UL - BC Intensity");
			materialEditor.ShaderProperty(FindProperty("_UL_N_tex"), "UL - N tex");
			materialEditor.ShaderProperty(FindProperty("______UL_N_Intensity"), "UL - N Intensity");
			materialEditor.ShaderProperty(FindProperty("_UL_Mask_tex"), "UL - Mask tex");
			materialEditor.ShaderProperty(FindProperty("______UL_Roughness_Intensity"), "UL - Roughness Intensity");
			materialEditor.ShaderProperty(FindProperty("______UL_Metallic_Intensity"), "UL - Metallic Intensity");
			materialEditor.ShaderProperty(FindProperty("______UL_AO_Intensity"), "UL - AO Intensity");
			materialEditor.ShaderProperty(FindProperty("______UL_AlphaClip_Intensity"), "UL - AlphaClip Intensity");

			EditorGUI.indentLevel = 0;
		}

		//================================================================
		//-------------------|   TERRAIN LAYERS    |----------------------
		//================================================================
		using (new GUILayout.VerticalScope(EditorStyles.helpBox))
		{
			EditorGUILayout.LabelField("TERRAIN LAYERS", styBoldCenter);

			NumberOfLayers layers = NumberOfLayers.OnePass;
			if (targetMat.IsKeywordEnabled("_LAYERS_ONE"))
			{
				layers = NumberOfLayers.OneLayer;
			}
			else if (targetMat.IsKeywordEnabled("_LAYERS_TWO"))
			{
				layers = NumberOfLayers.TwoLayers;
			}
			EditorGUI.BeginChangeCheck();
			layers = (NumberOfLayers)EditorGUILayout.EnumPopup(layers);
			if (EditorGUI.EndChangeCheck())
			{
				materialEditor.RegisterPropertyChangeUndo("InTerra Shader Variant");
				SetKeyword("_LAYERS_ONE", layers == NumberOfLayers.OneLayer);
				SetKeyword("_LAYERS_TWO", layers == NumberOfLayers.TwoLayers);
				if (!disableUpdates) InTerra_Data.UpdateTerrainData(updateDict);
			}

			//----------------------	ONE LAYER   ----------------------
			if (isOnTerrain && layers == NumberOfLayers.OneLayer)
			{
				SelectTerrainLayer(1, "Terrain Layer:");
			}

			//----------------------   TWO LAYERS   -----------------------
			if (isOnTerrain && layers == NumberOfLayers.TwoLayers)
			{
				SelectTerrainLayer(1, "Terrain Layer 1:");
				SelectTerrainLayer(2, "Terrain Layer 2:");
			}

			//----------------------   ONE PASS   -----------------------
			if (isOnTerrain && layers == NumberOfLayers.OnePass)
			{
				List<string> passes = new List<string>();
				int layersInPass = targetMat.HasProperty("_Control1") ? 8 : 4;
				int passNumber = (int)targetMat.GetFloat("_PassNumber");
				int passesList = passNumber + 1;
				if (terrain.terrainData.alphamapTextureCount <= passNumber)
				{
					EditorGUILayout.HelpBox("The Terrain do not have pass " + (passNumber + 1) + ".", MessageType.Warning);
				}
				else
				{
					passesList = (int)Mathf.Ceil((float)terrain.terrainData.alphamapLayers / layersInPass);
				}

				for (int i = 0; i < (passesList); i++)
				{
					passes.Add("Pass " + (i + 1).ToString() + " - Layers  " + (i * layersInPass + 1).ToString() + " - " + (i * layersInPass + layersInPass).ToString());
				}

				if (!targetMat.shader.name.Contains("InTerra/HDRP"))
				{
					EditorGUI.BeginChangeCheck();
					passNumber = EditorGUILayout.Popup(passNumber, passes.ToArray(), GUILayout.MinWidth(150));

					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra LayerNumber1");
						targetMat.SetFloat("_PassNumber", passNumber);
						if (!disableUpdates) InTerra_Data.UpdateTerrainData(updateDict);
					}
				}

				GUILayout.BeginHorizontal();
				for (int i = passNumber * layersInPass; i < (passNumber * layersInPass + layersInPass); i++)
				{
					string layerName = "Empty";
					Texture2D layerTexture = null;

					if (i < terrain.terrainData.alphamapLayers)
					{
						TerrainLayer tl = tLayers[i];
						if (tl)
						{
							layerName = tl.name;
							layerTexture = AssetPreview.GetAssetPreview(tl.diffuseTexture);
						}
						else
						{
							layerName = "Missing";
						}
					}
					if (i < terrain.terrainData.alphamapLayers)
					{
						using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(50)))
						{
							if (layerTexture)
							{
								GUI.DrawTexture(EditorGUILayout.GetControlRect(GUILayout.Width(48), GUILayout.Height(48)), layerTexture, ScaleMode.ScaleAndCrop);
							}
							else
							{
								EditorGUILayout.GetControlRect(GUILayout.Width(48), GUILayout.Height(48));
							}
							EditorGUILayout.LabelField(layerName, styleMini, GUILayout.Width(48), GUILayout.Height(12));
						}
					}

				}
				GUILayout.EndHorizontal();
			}
		}

		//============================================================================
		//-----------------------|  TERRAIN INTERSECTION  |---------------------------
		//============================================================================
		using (new GUILayout.VerticalScope(EditorStyles.helpBox))
		{
			EditorGUILayout.LabelField("TERRAIN INTERSECTION", styBoldCenter);
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUI.BeginChangeCheck();

				Vector4 intersection = InTerra_GUI.MinMaxValues(targetMat.GetVector("_Intersection"), false, ref minmax1);

				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra BlendingIntersection");
					targetMat.SetVector("_Intersection", intersection);
				}

				EditorGUILayout.Space();

				PropertyLine("_Sharpness", "Sharpness", "Sharpness of blending");

				EditorGUI.BeginChangeCheck();

				EditorGUI.indentLevel = 1;
				nIntersect = EditorGUILayout.Foldout(nIntersect, LabelAndTooltip("Mesh Normals Intersection", "The height of intersection of terrain's and object's mesh normals. This value is calculated per vertex and it always affects the whole polygon!"), true);
				EditorGUI.indentLevel = 0;
				if (nIntersect)
				{
					Vector4 normalIntersect = InTerra_GUI.MinMaxValues(targetMat.GetVector("_NormIntersect"), false, ref minmaxNi);

					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra NormalIntersection");
						targetMat.SetVector("_NormIntersect", normalIntersect);
					}

				}
			}

			//============================= STEEP SLOPES =============================
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Steep slopes", styBoldCenter);

				//------------------------- SECONDARY INTERSECTION  -------------------------------
				bool steepIntersect = targetMat.GetFloat("_SteepIntersection") == 1;

				EditorGUI.BeginChangeCheck();
				steepIntersect = EditorGUILayout.ToggleLeft(LabelAndTooltip("Secondary Intersection", "Separated intersection for steep slopes."), steepIntersect);
				Vector4 intersection2 = targetMat.GetVector("_Intersection2");

				if (steepIntersect)
				{
					intersection2 = InTerra_GUI.MinMaxValues(intersection2, false, ref minmax2);
					PropertyLine("_Steepness", "Steepness adjust", "This value adjusts the angle that will be considered as steep.");
				}
				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra Secondary Intersection");
					SetKeyword("_OBJECT_STEEP_INTERSECTION", steepIntersect);
					if (steepIntersect) targetMat.SetFloat("_SteepIntersection", 1); else targetMat.SetFloat("_SteepIntersection", 0);
					targetMat.SetVector("_Intersection2", intersection2);
				}

				//------------------------------ TRIPLANAR -------------------------------
				bool triplanar = targetMat.IsKeywordEnabled("_OBJECT_TRIPLANAR");
				bool disOffset = targetMat.GetFloat("_DisableOffsetY") == 1;

				EditorGUI.BeginChangeCheck();
				triplanar = EditorGUILayout.ToggleLeft(LabelAndTooltip("Triplanar Mapping", "The Texture on steep slopes of Object will not be stretched."), triplanar);

				if (triplanar)
				{
					EditorGUI.indentLevel = 1;
					EditorStyles.label.fontSize = 10;
					disOffset = EditorGUILayout.ToggleLeft(LabelAndTooltip("Disable Height and Position Offset", "Front and Side projection of texture is offsetting by position and height to fit the Terrain texture as much as possible, but in some cases, if there is too steep slope of terrain, it can get stretched and it is better to disable the offsetting."), disOffset, GUILayout.Width(200));
					EditorStyles.label.fontSize = 12;
					EditorGUI.indentLevel = 0;
				}
				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra Object Triplanar");
					SetKeyword("_OBJECT_TRIPLANAR", triplanar);
					if (disOffset) targetMat.SetFloat("_DisableOffsetY", 1); else targetMat.SetFloat("_DisableOffsetY", 0);
				}

				//------------------------------ DISTORTION -------------------------------
				if (!triplanar)
				{
					EditorStyles.label.fontSize = 11;
					materialEditor.ShaderProperty(FindProperty("_SteepDistortion"), LabelAndTooltip("Distortion (by Albedo)", "This value distorts stretched texture on Steep slopes, this is useful if you don't want to use triplanar - which is more performance heavy. Distortion is calculated by Albedo Texture and doesn't work with a single color."));
					EditorStyles.label.fontSize = 12;
				}
			}

			//------------------------------ DISABLE HIDE TILING -------------------------------
			if (terrain != null && terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_DISTANCEBLEND"))
			{
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					bool distanceBlend = targetMat.GetFloat("_DisableDistanceBlending") == 1;

					EditorGUI.BeginChangeCheck();
					distanceBlend = EditorGUILayout.ToggleLeft(LabelAndTooltip("Disable Hide Tiling", "If Terrain \"Hide Tiling\" is set on, this option will turn it off only for this Material to prevent additional samplings and calculations. This may cause some more or less visible seams in distance."), distanceBlend);

					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra Disable Hide Tiling");

						if (distanceBlend)
						{
							targetMat.SetFloat("_DisableDistanceBlending", 1);
							targetMat.DisableKeyword("_TERRAIN_DISTANCEBLEND");
						}
						else
						{
							targetMat.SetFloat("_DisableDistanceBlending", 0);
							SetKeyword("_TERRAIN_DISTANCEBLEND", terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_DISTANCEBLEND"));
						}
					}
				}
			}

			//------------------------------ DISABLE TERRAIN PARALLAX -------------------------------
			if (terrain != null && targetMat.shader.name != InTerra_Data.DiffuseObjectShaderName && terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_PARALLAX"))
			{
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					bool terrainParallax = targetMat.GetFloat("_DisableTerrainParallax") == 1;

					EditorGUI.BeginChangeCheck();
					terrainParallax = EditorGUILayout.ToggleLeft(LabelAndTooltip("Disable Terrain Parallax", "If Terrain \"Parallax Occlusion Mapping\" is set on, this option will turn it off only for this Material."), terrainParallax);

					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra Disable Terrain Parallax");

						if (terrainParallax)
						{
							targetMat.SetFloat("_DisableTerrainParallax", 1);
							targetMat.DisableKeyword("_TERRAIN_PARALLAX");
						}
						else
						{
							targetMat.SetFloat("_DisableTerrainParallax", 0);
							SetKeyword("_TERRAIN_PARALLAX", terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_PARALLAX"));
						}
					}
				}
			}
		}

		//============================================================================
		//----------------------------|  TESSELLATION  |------------------------------
		//============================================================================
		if (targetMat.shader.name.Contains(InTerra_Data.HDRPObjectTessellationShaderName))
		{
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("TESSELLATION", styBoldCenter);

				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					PropertyLine("_TessellationFactor", "Tessellation Factor", "Controls the strength of the tessellation effect. Higher values result in more tessellation. Maximum tessellation factor is 15 on the Xbox One and PS4");
					PropertyLine("_TessellationBackFaceCullEpsilon", "Triangle Culling Epsilon", "Controls triangle culling. A value of -1.0 disables back face culling for tessellation, higher values produce more aggressive culling and better performance.");
					PropertyLine("_TessellationFactorTriangleSize", "Triangle Size", "Sets the desired screen space size of triangles (in pixels). Smaller values result in smaller triangle. Set to 0 to disable adaptative factor with screen space size.");

					TessellationMode tessMode = targetMat.IsKeywordEnabled("_TESSELLATION_PHONG") ? TessellationMode.Phong : TessellationMode.None;

					EditorGUI.BeginChangeCheck();
					using (new GUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField(LabelAndTooltip("Tessellation Mode", "Specifies the method HDRP uses to tessellate the mesh. None uses only the Displacement Map to tessellate the mesh. Phong tessellation applies additional Phong tessellation interpolation for smoother mesh."), GUILayout.Width(120));
						tessMode = (TessellationMode)EditorGUILayout.EnumPopup(tessMode);
					}

					if (tessMode == TessellationMode.Phong)
					{
						PropertyLine("_TessellationShapeFactor", "Shape Factor", "Controls the strength of Phong tessellation shape (lerp factor).");
					}
					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra Object Tessellation Mode");
						SetKeyword("_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
					}

					if (terrain.materialTemplate.shader.name.Contains(InTerra_Data.TessellationShaderFolder))
					{
						EditorGUILayout.HelpBox("The setting of distance fading and shadows quality is synchronized with the Terrain setting.", MessageType.Info);
					}
					else
					{
						PropertyLine("_TessellationShadowQuality", "Shadows quality", "Setting of shadows accuracy calculation. Higher value means more precise calculation.");
						EditorGUI.indentLevel = 1;
						tessDistances = EditorGUILayout.Foldout(tessDistances, "Fading Distances", true);
						EditorGUI.indentLevel = 0;
						if (tessDistances)
						{
							InTerra_GUI.TessellationDistaces(targetMat, materialEditor, ref minmaxMip);
						}
					}
				}
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					EditorGUILayout.LabelField("Displacement", styleBoldLeft);

					float tessDisplacement = targetMat.GetFloat("_TessellationDisplacement") * 100;
					float tessOffset = targetMat.GetFloat("_TessellationOffset") * 100;
					float terrainTessOffset = targetMat.GetFloat("_TerrainTessOffset") * 100;

					EditorGUI.BeginChangeCheck();

					tessDisplacement = EditorGUILayout.FloatField(LabelAndTooltip("Amplitude", "Amplitude of the Height Map (Blue channel in Mask Map)."), tessDisplacement);
					tessOffset = EditorGUILayout.FloatField(LabelAndTooltip("Height Offset", "Height offset for displacement."), tessOffset);
					terrainTessOffset = EditorGUILayout.FloatField(LabelAndTooltip("Terrain Layers Offset", " Offset for Terrain Layers displacement."), terrainTessOffset);

					tessDisplacement = Mathf.Clamp(tessDisplacement, 0, 50) * 0.01f;
					tessOffset = Mathf.Clamp(tessOffset, -50, 50) * 0.01f;
					terrainTessOffset = Mathf.Clamp(terrainTessOffset, -50, 50) * 0.01f;

					if (EditorGUI.EndChangeCheck())
					{
						materialEditor.RegisterPropertyChangeUndo("InTerra Object Tessellation Properties");
						targetMat.SetFloat("_TessellationDisplacement", tessDisplacement);
						targetMat.SetFloat("_TessellationOffset", tessOffset);
						targetMat.SetFloat("_TerrainTessOffset", terrainTessOffset);

						float terrainMaxDisplacement = terrain.materialTemplate.GetFloat("_TessellationMaxDisplacement");
						float objectMaxDisplacement = (tessDisplacement / 2) + tessOffset + terrainTessOffset;

						float maxDisplacement = objectMaxDisplacement > terrainMaxDisplacement ? objectMaxDisplacement : terrainMaxDisplacement;

						targetMat.SetFloat("_TessellationObjMaxDisplacement", objectMaxDisplacement);
						targetMat.SetFloat("_TessellationMaxDisplacement", maxDisplacement);
					}
				}
				PropertyLine("_Tessellation_Sharpness", "Blending Sharpness", "Heightmap blending sharpness between Terrains and Objects Textures for Tessellation.");
			}
		}

		//================= TERRAIN INFO ================
		EditorGUI.indentLevel = 1;
		terrainInfo = EditorGUILayout.Foldout(terrainInfo, "Terrain info", true);
		EditorGUI.indentLevel = 0;
		if (terrainInfo && isOnTerrain)
		{
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Name:", styleBoldLeft, GUILayout.Width(60));
				EditorGUILayout.LabelField(terrain.name, styleLeft, GUILayout.MinWidth(50));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Position:", styleBoldLeft, GUILayout.Width(60));
				Vector3 tp = terrain.GetPosition();

				EditorGUILayout.LabelField("X: " + tp.x.ToString(), styleLeft, GUILayout.MinWidth(50));
				EditorGUILayout.LabelField("Y: " + tp.y.ToString(), styleLeft, GUILayout.MinWidth(50));
				EditorGUILayout.LabelField("Z: " + tp.z.ToString(), styleLeft, GUILayout.MinWidth(50));
				GUILayout.EndHorizontal();
			}
			EditorGUI.indentLevel = 0;
		}
		GUI.enabled = true;

		//================= OBJECT INFO ================
		EditorGUI.indentLevel = 1;
		objectInfo = EditorGUILayout.Foldout(objectInfo, "Objects info", true);
		EditorGUI.indentLevel = 0;
		if (objectInfo)
		{
			if (!objectInfoInit)
			{
				if (!disableUpdates) InTerra_Data.UpdateTerrainData(true);
				if (materialTerrain.ContainsKey(targetMat))
				{
					GetTerrain();
					CreateObjectsLists(targetMat, terrain);
				}
				objectInfoInit = true;
			}
			if (!updateAtOpen)
			{
				WrongTerrainWarning(terrain);
			}

			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
				{
					GUILayout.Label("Name", styleMiniBold, GUILayout.MinWidth(60));
					GUILayout.Label("position (x,y,z)", styleMiniBold, GUILayout.MinWidth(40));
					GUILayout.Label("Go to Object", styleMiniBold, GUILayout.Width(65));
				}

				ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos, GUILayout.Height(100));

				ObjectsList(noTerrain, Color.red);
				ObjectsList(wrongTerrain, new Color(1.0f, 0.5f, 0.0f));
				ObjectsList(okTerrain, Color.black);

				EditorGUILayout.EndScrollView();
			}
			EditorGUI.indentLevel = 0;
		}
		GUI.enabled = true;

		using (new GUILayout.VerticalScope(EditorStyles.helpBox))
		{
			if (GUILayout.Button("Update Terrain Data", styleButtonBold))
			{
				InTerra_Data.UpdateTerrainData(true);

				if (terrains.Length > 0 && materialTerrain.ContainsKey(targetMat))
				{
					GetTerrain();
					CreateObjectsLists(targetMat, terrain);
				}
			}
		}

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		//-------------------------------------------------------------
		materialEditor.RenderQueueField();
		materialEditor.EnableInstancingField();
		materialEditor.DoubleSidedGIField();
		//-------------------------------------------------------------

		//========================================================================
		//---------------------------|   WARNINGS   |-----------------------------
		//========================================================================
		void WrongTerrainWarning(Terrain terrain)
		{
			if (terrain != null)
			{
				if (noTerrain.Count > 0 && noTerrain.Count < 2)
				{
					EditorGUILayout.HelpBox("The Object " + noTerrain[0].name + " with this material is outside of any Terrain!", MessageType.Warning);
				}

				if (noTerrain.Count > 1)
				{
					EditorGUILayout.HelpBox("Some Objects with this material are outside of any Terrain!", MessageType.Warning);
				}

				if (wrongTerrain.Count > 0 && wrongTerrain.Count < 2)
				{
					EditorGUILayout.HelpBox("The Object " + wrongTerrain[0].name + " with this material is not on correct Terrain!", MessageType.Warning);
				}

				if (wrongTerrain.Count > 1)
				{
					EditorGUILayout.HelpBox("Some Objects with this material are not on correct Terrain!", MessageType.Warning);
				}
			}
		}

		//=====================================================================================
		//=====================================================================================
		void PropertyLine(string property, string label, string tooltip = null)
		{
			materialEditor.ShaderProperty(FindProperty(property), new GUIContent() { text = label, tooltip = tooltip });
		}

		void TextureSingleLine(string property1, string property2, string label, string tooltip = null)
		{
			materialEditor.TexturePropertySingleLine(new GUIContent() { text = label, tooltip = tooltip }, FindProperty(property1), FindProperty(property2));
		}

		GUIContent LabelAndTooltip(string label, string tooltip)
		{
			return new GUIContent() { text = label, tooltip = tooltip };
		}

		void SetKeyword(string name, bool set)
		{
			if (set) targetMat.EnableKeyword(name); else targetMat.DisableKeyword(name);
		}

		void RemapMask(ref float offset, ref float scale, string label, string tooltip = null)
		{
			using (new GUILayout.HorizontalScope())
			{
				scale += offset;
				EditorGUILayout.LabelField(new GUIContent() { text = label, tooltip = tooltip }, GUILayout.Width(100));
				EditorGUILayout.LabelField(" ", GUILayout.Width(3));
				EditorGUILayout.MinMaxSlider(ref offset, ref scale, 0, 1);
				scale -= offset;
			}
		}

		void GetTerrain()
		{
			if (materialTerrain.ContainsKey(targetMat) && materialTerrain[targetMat] != null)
			{
				terrain = materialTerrain[targetMat];
				isOnTerrain = true;
				tLayers = terrain.terrainData.terrainLayers;
			}
			else
			{
				terrain = null;
				isOnTerrain = false;
				tLayers = null;
			}
		}

		void SelectTerrainLayer(int layerNumber, string label)
		{
			string tagName = "TerrainLayerGUID_" + layerNumber.ToString();
			TerrainLayer terainLayer = InTerra_Data.TerrainLayerFromGUID(targetMat, tagName);

			EditorGUI.BeginChangeCheck();

			using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField(LabelAndTooltip(label, "The Terrain Layer the Material will be blended with"), styleLeft, GUILayout.MaxWidth(100));
				Rect rt = GUILayoutUtility.GetLastRect();
				if (terainLayer && AssetPreview.GetAssetPreview(terainLayer.diffuseTexture))
				{
					GUI.DrawTexture(new Rect(rt.x + 103, rt.y, 21, 21), AssetPreview.GetAssetPreview(terainLayer.diffuseTexture), ScaleMode.ScaleToFit, true);
				}

				EditorGUILayout.GetControlRect(GUILayout.Width(20));
				terainLayer = (TerrainLayer)EditorGUILayout.ObjectField(terainLayer, typeof(TerrainLayer), false, GUILayout.MinWidth(100), GUILayout.Height(22));

				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra TerrainLayer");
					targetMat.SetOverrideTag(tagName, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terainLayer)));

					if (!disableUpdates) InTerra_Data.UpdateTerrainData(updateDict);
				}
			}

		}
	}
	//------------- Functions ------------------------
	MaterialProperty FindProperty(string name)
	{
		return FindProperty(name, properties);
	}

	void ObjectsList(List<Renderer> rend, Color color)
	{

		var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
		if (color != Color.black)
		{
			style.normal.textColor = color;
		}

		for (int i = 0; i < rend.Count; i++)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(rend[i].name, style, GUILayout.MinWidth(60));
			GUILayout.Label(rend[i].bounds.center.x.ToString() + ", " + rend[i].bounds.center.y.ToString() + ", " + rend[i].bounds.center.z.ToString(), style, GUILayout.MinWidth(40));

			if (GUILayout.Button("  -->  ", EditorStyles.miniButton, GUILayout.Width(50)))
			{
				Selection.activeGameObject = rend[i].gameObject;
				SceneView.lastActiveSceneView.Frame(rend[i].bounds, false);
			}
			GUILayout.EndHorizontal();
		}
	}

	void CreateObjectsLists(Material targetMat, Terrain terain)
	{
		Terrain[] terrains = Terrain.activeTerrains;
		MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

		okTerrain.Clear();
		noTerrain.Clear();
		wrongTerrain.Clear();

		foreach (Renderer rend in renderers)
		{
			if (rend != null && rend.transform.position != null)
			{
				foreach (Material mat in rend.sharedMaterials)
				{
					if (mat != null && mat.shader != null && mat.shader.name != null && (mat == targetMat))
					{
						noTerrain.Add(rend); //it is easier to check if the renderer is on Terrain, so all renderes will be add to this list and if it is on terrain, it will be removed 
						wrongTerrain.Add(rend);

						Vector2 pos = new Vector2(rend.bounds.center.x, rend.bounds.center.z);

						if (InTerra_Data.CheckPosition(terain, pos))
						{
							okTerrain.Add(rend);
							wrongTerrain.Remove(rend);
						}

						foreach (Terrain ter in terrains)
						{
							if (InTerra_Data.CheckPosition(ter, pos))
							{
								noTerrain.Remove(rend);
							}
						}
					}
				}
			}
		}

		foreach (Renderer nt in noTerrain)
		{
			wrongTerrain.Remove(nt);
		}
	}

}
