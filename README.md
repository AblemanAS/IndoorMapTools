# IndoorMapTools

IndoorMapTools is a research-oriented tool for constructing and analyzing indoor map layouts
for **collaborative indoor positioning systems**.

This repository contains the reference implementation of the framework proposed in:

> Son, Kyuho, and Dong-Soo Han.
> **A Framework for Indoor Map Layout Construction in Collaborative Positioning:
> Optimized Analysis of Reachability and Vertical Transitions.**
> IEEE SMC 2025 VIENNA-AUSTRIA. IEEE SMC 2025, 2025.
---

## Summary

The tool supports manual construction and validation of indoor map layouts with explicit
representation of spatial connectivity.

Key features include:

- Definition and management of indoor spatial entities (Building, Floor, Landmark, Landmark Group)
- Raster-based reachable area labeling
- Generation of binary (1bpp) Occupancy Grid Maps (OGM)
- Reachability graph construction across floors
- Connectivity analysis using reachable clustering
- Visualization and optimization using the Floor–Group–Area (FGA) Matrix

The framework is designed to support **fine-grained connectivity modeling**, including
**vertical transitions** such as stairs, elevators, and escalators, which are often insufficiently
handled in conventional indoor mapping tools.

---

## Output

Projects can be exported as:

- **IMPJ (Indoor Map Project JSON)**: structured metadata in a ZIP archive

---

## Performance

All computationally intensive operations (OGM generation, reachability analysis, clustering,
and FGA-based group reordering) are designed to run within a few seconds on a standard PC,
as reported in the paper.

---

## Purpose

This repository is intended for:

- Research on indoor map modeling and connectivity analysis
- Reproducibility of the algorithms described in the paper
- Development of collaborative indoor positioning systems

---

## Reference

If you use this software in academic work, please cite the corresponding paper.

---

## TODOs
- Reachable area marking algorithm in Single mark (Flood Fill) performance enhancement
- OGM segmentation algorithm performance enhancement
- Option to limit maximum resolution on mapimages
- Bring-into-view feature for analyzed export reachability in map view on selection
- Visualizing feature for isolated areas
- Separating MouseToolEventArgs as DTO