# Real-Time Geometric Modeling: Winged Edge & Catmull-Clark

This Unity project provides a robust implementation of the Winged Edge topological data structure and the Catmull-Clark subdivision surface algorithm. It is designed to generate and process 3D meshes for geometric modeling applications.

## Live Demo

The following animations demonstrate the Catmull-Clark subdivision algorithm being applied to a simple mesh.

| Chips                                                                                 | Cap                                                        | Box                                                                                   |
|---------------------------------------------------------------------------------------|------------------------------------------------------------|---------------------------------------------------------------------------------------|
| ![Catmull-Clark Subdivision Animation_Chips](./Demo/chips.gif) | ![Catmull-Clark Subdivision Animation_Box](./Demo/cap.gif) | ![Catmull-Clark Subdivision Animation_QuadBox](./Demo/box.gif) |

---

## Table of Contents
1.  [Overview](#1-overview)
2.  [Core Components](#2-core-components)
3.  [Getting Started](#3-getting-started)
4.  [Technical Details & Limitations](#4-technical-details--limitations)

## 1. Overview

The primary goal of this project is to explore advanced 3D mesh manipulation techniques within the Unity engine. It was developed as part of the **E4FI - Geometric Modeling** course at ESIEE, focusing on the practical application of data structures and algorithms in computer graphics.

The system is built around a custom, non-destructive `WingedEdgeMesh` data structure, which enables efficient topological queries and modifications required for algorithms like Catmull-Clark.

Key capabilities include:
-   Procedural generation of base control meshes (e.g., Box, Chips, QuadBox).
-   Conversion from standard `UnityEngine.Mesh` objects to the `WingedEdgeMesh` representation.
-   A full implementation of the Catmull-Clark subdivision algorithm, including support for boundary conditions.
-   An in-editor visualization tool, `WingedEdgeDebugger`, for inspecting the topological data.

## 2. Core Components

### Winged Edge Data Structure
The `WingedEdgeMesh` class is the foundation of this project. It is a topological data structure where each edge stores explicit references to its start and end vertices, its left and right adjacent faces, and its four neighboring "wing" edges. This structure makes complex topological queries, such as finding all edges incident to a vertex, highly efficient. The constructor performs strict 2-manifold validation to ensure topological integrity and automatically "weaves" boundary edge pointers using clockwise winding order convention.

### Catmull-Clark Subdivision
The `CatmullClarkSubdivider` component implements the Catmull-Clark subdivision algorithm. It operates on a `WingedEdgeMesh` and generates a new, smoother mesh. The process is iterative and non-destructive to the original data. It correctly calculates new Face Points, Edge Points, and updated Vertex Points according to the standard algorithm, with specific rules for handling boundary cases.

### Unity Editor Integration
The project is designed to be used directly within the Unity Editor. A `MeshBehaviour` component acts as the initial mesh generator, while the `CatmullClarkSubdivider` component provides an Inspector-based interface to apply subdivision levels and revert to the original mesh. A custom `WingedEdgeDebugger` component allows for direct visualization of the `WingedEdgeMesh` data (vertices, edges, faces, and their IDs) using Unity's Gizmo system.

## 3. Getting Started

1.  Open the project in the Unity Editor.
2.  Create a new empty GameObject in the scene.
3.  Add the following components to the GameObject using the Inspector's "Add Component" button:
    *   `Mesh Filter`
    *   `Mesh Renderer`
    *   `Mesh Behaviour`
    *   `Catmull Clark Subdivider`
    *   `Winged Edge Debugger`
4.  **Configure `Mesh Behaviour`**. Select a `Mesh Type To Create` from the dropdown menu to generate the initial control mesh.
5.  **Configure `Catmull Clark Subdivider`**. This component controls the subdivision process.
    *   Click **Save Original Mesh** to import the generated mesh into the internal `WingedEdgeMesh` structure. This action also makes the mesh data available to the `WingedEdgeDebugger`.
    *   Set the desired number of **Subdivision Levels**.
    *   Click **Apply Catmull-Clark** to execute the subdivision. The mesh displayed in the scene will update to the new, subdivided version.
    *   Click **Revert to Original** to restore the initial mesh.
6.  **Use `Winged Edge Debugger`**. Enable or disable the various `show...` toggles in the Inspector to visualize the vertices, edges, faces, and their respective IDs of the current `WingedEdgeMesh`. Ensure Gizmos are enabled in the Scene view toolbar.

## 4. Technical Details & Limitations

*   **2-Manifold Topology**: The `WingedEdgeMesh` constructor strictly enforces a 2-manifold topology. This strict enforcement is a deliberate design choice to keep the conversion process from `UnityEngine.Mesh` **purely topological**. It avoids the need for geometric heuristics (e.g., angle sorting) to resolve ambiguities found in non-2-manifold structures. The system will throw an `ArgumentException` for any non-2-manifold input.


*   **Supported Input & Polygon Agnosticism**: The `WingedEdgeMeshBuilder` can import data directly from `UnityEngine.Mesh` objects, which are inherently limited by the Unity engine to `MeshTopology.Triangles` and `MeshTopology.Quads`. However, the core `WingedEdgeMesh` structure and its construction logic are designed to be **polygon-agnostic**. The `Catmull-Clark` implementation will also correctly process any valid polygonal mesh provided. To support arbitrary n-gons (polygons with > 4 sides), one would need to provide the data from a source other than a standard `UnityEngine.Mesh`, for example by writing a custom parser for a file format like `.obj` that natively supports n-gons.


*   **Boundary Vertices**: The boundary weaving algorithm assumes that each boundary vertex is incident to exactly two **boundary** edges. Vertices where more than two boundary edges meet are considered non-2-manifold and will cause an exception. This limitation is consistent with the goal of maintaining a purely topological validation system.