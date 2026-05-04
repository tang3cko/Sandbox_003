# 08_voxel_terrain_godot

Donkey Kong Bananza 風の「削れる地形」。Marching Cubes による滑らかボクセル本実装。
立方体ボクセル（旧BlockyMeshBuilder）も並存しているが、現在 Main は MarchingCubesMesher を使用。

---

## Architecture

```
Scripts/
├── Core/                          (Godot 非依存・xUnit でテスト可能)
│   ├── 立方体（旧）
│   │   ├── MaterialId.cs              - byte 定数
│   │   ├── MaterialPalette.cs         - ID → 色
│   │   ├── ChunkData.cs               - byte ボクセル配列
│   │   ├── VoxelEdits.cs              - 立方体用編集
│   │   ├── BlockyMeshBuilder.cs       - face culling メッシャー
│   │   └── IVoxelMesher.cs            - 旧戦略インターフェース
│   ├── 滑らか（本実装）
│   │   ├── DensityField.cs            - float 3D 配列 (符号付き場)
│   │   ├── SdfEdits.cs                - SDF 合成 (DigSphere/PlaceSphere)
│   │   ├── MarchingCubesTables.cs     - 256 ケース edge/triangle テーブル
│   │   └── MarchingCubesMesher.cs     - MC 実装
│   └── MeshData.cs                    - 共通メッシュ中間表現
├── World/                         (Godot 統合)
│   ├── VoxelChunk.cs                  - DensityField + MC で MeshInstance3D 生成
│   └── VoxelWorld.cs                  - チャンク集約 + DigSphere/PlaceSphere API
├── Player/
│   ├── FreeCameraPlayer.cs            - フライカメラ
│   └── DiggingTool.cs                 - レイキャスト → 球状削り
└── Main.cs                            - シーン構築
```

### 密度場の規約

| 値 | 意味 |
|----|------|
| `density > 0` | 固体（地形） |
| `density < 0` | 空気 |
| `density == 0` | 表面 |

これは「正値内側」型の implicit field。標準 SDF（負値内側）の符号反転版で、ブール演算が直感的：

| 操作 | 式 |
|------|----|
| Union (Place)     | `new = max(old, brush)` |
| Subtract (Dig)    | `new = min(old, -brush)` |

球ブラシ：`brush(p) = radius - distance(p, center)`（球内が正、外が負）

### Marching Cubes テーブル出典

[dwilliamson の MC Lookup Tables gist](https://gist.github.com/dwilliamson/c041e3454a713e58baf6e4f8e5fffecd) を verbatim で `MarchingCubesTables.cs` に転記。

座標規約：corner i の位置 = `(i&1, (i&2)>>1, (i&4)>>2)`

### Triangle Winding と Godot 規約

- MC テーブルが生成する三角形は「ソリッド側から見て CCW」（自然法線が外向き）
- 空気側のプレイヤーから見ると CCW（同じ winding を反対側から見ると CW になる、と思いきや方向ベクトルの問題でこれは CCW のまま）
- Godot は **CW = 表面**（[公式ドキュメント](https://docs.godotengine.org/en/stable/classes/class_surfacetool.html)）
- ゆえに `MarchingCubesMesher` ではテーブルから読んだ `(a, b, c)` を `(a, c, b)` に並べ替えて発行 → 空気側から CW = 表面

per-vertex normal は中心差分による密度勾配の負号 `-∇d` を正規化（外向き）。

---

## Controls

| 入力 | 動作 |
|------|------|
| WASD | 移動 |
| Space / Shift | 上昇 / 下降 |
| マウス | 視点 |
| 左クリック | 削る (DigSphere, 半径2.0) |
| 右クリック | 置く (PlaceSphere, 半径1.5) |
| Esc | マウスキャプチャ切替 |

---

## Tests

35 テスト：

| ファイル | 件数 | 対象 |
|----------|------|------|
| `ChunkDataTests` | 5 | 旧 byte ボクセル |
| `VoxelEditsTests` | 8 | 旧 byte 編集 |
| `BlockyMeshBuilderTests` | 5 | 旧 face culling + winding |
| `DensityFieldTests` | 5 | float 場 |
| `SdfEditsTests` | 5 | SDF 編集 |
| `MarchingCubesMesherTests` | 7 | MC メッシュ + winding + 法線方向 |

```bash
cd Tests
dotnet test
```

---

## Build

```bash
dotnet build 08_voxel_terrain_godot.csproj
```
