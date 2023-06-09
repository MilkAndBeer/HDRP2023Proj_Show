# Getting started with the High Definition Render Pipeline

The [High Definition Render Pipeline (HDRP)](index.md) uses [Shaders](https://docs.unity3d.com/Manual/class-Shader.html) and lighting units that are different to those in Unity's built-in render pipeline. This means that you must either create a new Project that uses HDRP, or [upgrade an existing Project to use HDRP](#UpgradingToHDRP).

This page describes how to create a Scene that uses HDRP, and introduces you to key features that help you produce high fidelity visuals.

<a name="UpgradingToHDRP"></a>

To upgrade an existing Project that doesn’t use HDRP, you need to convert the Materials to make them compatible with HDRP. For information about the upgrade process, see [Upgrading to HDRP](Upgrading-To-HDRP.md).

## Creating an HDRP Project from the Template

To set up and manage your Unity Projects, install the [Unity Hub](https://docs.unity3d.com/hub/manual/index.html).

Unity provides an HDRP Template Project which you can use to get set up with HDRP. To create an HDRP Template Project:

1. Open the Unity Hub,
2. Go to **Projects**, and select **New Project**.
3. Open the **Sample** tab and select **3D Sample Scene (HDRP)**.
4. Enter a **Project name**.
5. Select **Create project**.

Unity creates a Project and automatically installs the HDRP package, and all its dependencies. After Unity opens the Template Project, you can see the main Scene.

![](Images/GettingStarted1.png)

This template is a great starting point for projects aiming for high-end graphics. It includes multiple physically based lighting setups to help you realistically illuminate environments with HDRP. It also includes examples for many of HDRP's features such as [Decals](Decal.md), [Volumes](Volumes.md), and physically accurate Materials.

## Pipeline setup

Before you can use HDRP, you need an HDRP Asset, which controls the global rendering settings and creates an instance of the High Definition Render Pipeline. The **High-Definition RP** Template creates an HDRP Asset for you, but you can create different HDRP Assets to suit your rendering needs, such as one HDRP Asset for every target platform. An HDRP Asset allows you to enable features for your entire Project in the Editor. It allocates memory for the different features, so you can't edit them at runtime. For more information, see [HDRP Asset](HDRP-Asset.md).

To find the right balance between render quality and runtime performance, adjust the [Frame Settings](Frame-Settings.md) for your [Cameras](HDRP-Camera.md). Frame Settings allow you to enable or disable effects at runtime on a per-Camera basis, as long as you enable the effect in the HDRP Asset before entering Play Mode or building your HDRP Project.

## Render Pipeline Wizard

HDRP provides you with the [Render Pipeline Wizard](Render-Pipeline-Wizard.md) to help you set up your Project with HDRP. You can also use it to add support for DirectX Raytracing (DXR) or VR to your HDRP Project. If you use the **High-Definition RP** Template to create your Project, you don't need to use the Render Pipeline Wizard, unless you want to use DXR or VR.

## Volumes

[Volumes](Volumes.md) allow you to partition your Scene into areas so you can control lighting and effects at a finer level, rather than tuning an entire Scene. You can add as many volumes to your Scene as you want, to create different spaces, and then light them all individually for a realistic effect. Each volume has an environment, so you can adjust its sky, fog, and shadow settings. You can also create custom [Volume Profiles](Volume-Profile.md) and switch between them.

To add a Volume to your Scene and edit its Volume Profile:

1. Go to **GameObject** > **Volume** and select one of the options from the list.
2. In the Scene or Hierarchy view, select the new GameObject to view it in the Inspector.
3. In the **Volume** component, assign a Volume Profile to the **Profile** property field. If you want to create a new Volume Profile, select **New** next to the property field.
4. The list of [Volume overrides](Volume-Components.md) that the Volume Profile contains appears below the **Profile** property. Here you can add or remove Volume overrides and edit their properties.

### Visual Environment

The [Visual Environment](Override-Visual-Environment.md) override allows you to change the sky and fog you want in a Scene. For example, use volumetric fog to create atmospheric light rays:

![](Images/GettingStarted3.png)

For more information, see [Visual Environment](Override-Visual-Environment.md), [Sky overview](HDRP-Features.md#sky), and [Fog overview](HDRP-Features.md#fog).

## Materials and Shaders

HDRP provides Shaders you can use to create a wide variety of different Materials. For example, you can create glass with a refractive effect or leaves with subsurface scattering. The options for a Material depend on which Shader the Material uses. HDRP shares many Material properties across Shaders. For more information, see [HDRP Material features](HDRP-Features.md#Material).

## Lighting

To apply realistic lighting to your Scenes, HDRP uses Physical Light Units (PLU), which are based on real-life measurable values, just like you would see when looking for light bulbs at a store or measuring light with a photographic light meter.

**Note**: For Lights to behave properly when using PLU, you need to use the HDRP unit convention (1 Unity unit equals 1 meter). For more information, see [Physical Light Units](Physical-Light-Units.md).

**Note**: The HDRI sky used by HDRP has a default exposure of 10. However, newly created directional Lights have an intensity of 3.14 which can cause objects to look black because of the auto exposure compensating for the overly bright sky. Set a value of 10000 for your directional Light for a good effect for a mix of indoor and outdoor scenes. If you set up the HDRP wizard properly, newly created scenes should have coherent values by default.

For more information, see [HDRP Lighting features](HDRP-Features.md#Lighting). For advice on adding lights to your Scene, see [Light](Light-Component.md).

### Light Explorer

HDRP adds settings to the [Light Explorer](https://docs.unity3d.com/Manual/LightingExplorer.html) (menu: **Window** > **Rendering** > **Light Explorer**), so that you can adjust HDRP features and lighting units. ![](Images/GettingStarted4.png)

Use the Light Explorer to change the settings of any Light in your Project without the need to locate the Lights in the Scene. You can also manage Reflection Probes and Light Probes from this window.

## Shadows

The [Shadows](Override-Shadows.md) Volume override allows you to determine the overall quality of the Shadows in a Volume. For example, the **Max Distance** field calculates the quality of the Shadows based on the distance of the Camera from the Shadow.

For more information, see [Shadows](Override-Shadows.md).

## Related information

- For the full list of HDRP features, see [HDRP Features](HDRP-Features.md).
- For an online session on how to achieve real-time high-fidelity graphics in HDRP, see [Achieving High Fidelity Graphics for Games with HDRP](https://resources.unity.com/unitenow/onlinesessions/achieving-high-fidelity-graphics-for-games-with-hdrp).
