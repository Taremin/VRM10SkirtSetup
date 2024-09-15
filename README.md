# VRM10SkirtSetup

これは `VRM1.0` のスカートの `SpringBone` 設定を行う Unity エディタ拡張です。


## 使い方

1. アバターのルートに `VRMInstance` コンポーネントを付ける
2. スカートの根本となるボーンを `SkirtRoot` に設定
3. オフセット、Leg半径の設定を行う
    - オフセット: スカートの根本から Joint の設定を行うボーンまでの深さ
    - UpperLeg半径: UpperLeg にコライダーをつける時のボーンからの距離
4. スカートにつける Joint の設定を行う
5. コライダーの設定する
    - Radius:
        - コライダーの半径
        - 大きなコライダーでPlaneコライダーと同じことをするのである程度大きくしてください
    - YOffcet:
        - `UpperLeg` の根本方面への長さ
    - TailYOffset:
      - `UpperLeg` の先端方面への長さ
5. `Run` ボタンを押す

## 必要パッケージ

  - UniVRM: VRM 1.0 Import/Export

## 注意事項

このエディタ拡張によるセットアップを行うと以下の削除処理を最初に行います。
残しておきたいものがある場合は複製を作るなど予備を用意してから実行してください。

  - `VRM10SkirtSetup_` から始まる名前のゲームオブジェクト
  - `VRMInstance` に含まれる `VRM10SkirtSetup_` から始まる `SpringBone.ColliderGroups` 設定
  - `VRMInstance` に含まれる `VRM10SkirtSetup_` から始まる `SpringBone.Spring` 設定
  - `SkirtRoot` 以下のゲームオブジェクトについている `VRMSpringBoneJoint`

## ライセンス

[MIT](./LICENSE)
