;; mts -> main to side
;; stm -> side to main
(module
  (type $getIntType (func (param $addr i32) (result i32)))
  (type $getIntType64 (func (param $addr i32) (result i64)))
  (type $setIntType (func (param $addr i32) (param $value i32)))
  (type $setIntType64 (func (param $addr i32) (param $value i64)))
  (type $addFnPtrType (func (param $fPtr i32) (result i32)))
  (type $memcpyType (func (param $dest i32) (param $src i32) (param $size i32)))
  (type $fixedMemcpyType (func (param $dest i32) (param $src i32)))

  (import "main" "getI8" (func $mainGetI8 (type $getIntType)))
  (import "main" "getI16" (func $mainGetI16 (type $getIntType)))
  (import "main" "getI32" (func $mainGetI32 (type $getIntType)))
  (import "main" "getI64" (func $mainGetI64 (type $getIntType64)))
  (import "main" "setI8" (func $mainSetI8 (type $setIntType)))
  (import "main" "setI16" (func $mainSetI16 (type $setIntType)))
  (import "main" "setI32" (func $mainSetI32 (type $setIntType)))
  (import "main" "setI64" (func $mainSetI64 (type $setIntType64)))

  (import "side" "getI8" (func $sideGetI8 (type $getIntType)))
  (import "side" "getI16" (func $sideGetI16 (type $getIntType)))
  (import "side" "getI32" (func $sideGetI32 (type $getIntType)))
  (import "side" "getI64" (func $sideGetI64 (type $getIntType64)))
  (import "side" "setI8" (func $sideSetI8 (type $setIntType)))
  (import "side" "setI16" (func $sideSetI16 (type $setIntType)))
  (import "side" "setI32" (func $sideSetI32 (type $setIntType)))
  (import "side" "setI64" (func $sideSetI64 (type $setIntType64)))

  (import "mainToSide" "addFnPtr" (func $mts_addFnPtr (type $addFnPtrType)))
  (export "mts_addFnPtr" (func $mts_addFnPtr))

  (import "sideToMain" "addFnPtr" (func $stm_addFnPtr (type $addFnPtrType)))
  (export "stm_addFnPtr" (func $stm_addFnPtr))

  (func (export "mts_memcpy1") (type $fixedMemcpyType) 
    (call $sideSetI8 (local.get 0) (call $mainGetI8 (local.get 1)))
  )
  (func (export "mts_memcpy2") (type $fixedMemcpyType) 
    (call $sideSetI16 (local.get 0) (call $mainGetI16 (local.get 1)))
  )
  (func (export "mts_memcpy4") (type $fixedMemcpyType) 
    (call $sideSetI32 (local.get 0) (call $mainGetI32 (local.get 1)))
  )
  (func (export "mts_memcpy8") (type $fixedMemcpyType) 
    (call $sideSetI64 (local.get 0) (call $mainGetI64 (local.get 1)))
  )
 
  (func (export "stm_memcpy1") (type $fixedMemcpyType) 
    (call $mainSetI8 (local.get 0) (call $sideGetI8 (local.get 1)))
  )
  (func (export "stm_memcpy2") (type $fixedMemcpyType) 
    (call $mainSetI16 (local.get 0) (call $sideGetI16 (local.get 1)))
  )
  (func (export "stm_memcpy4") (type $fixedMemcpyType) 
    (call $mainSetI32 (local.get 0) (call $sideGetI32 (local.get 1)))
  )
  (func (export "stm_memcpy8") (type $fixedMemcpyType) 
    (call $mainSetI64 (local.get 0) (call $sideGetI64 (local.get 1)))
  )

  (func $mts_memcpy (export "mts_memcpy") (type $memcpyType)
    ;; 0: dest, 1: src, 2: size
    (if 
      (i32.eq
        (i32.and (local.get 0) (i32.const 7))
        (i32.and (local.get 1) (i32.const 7)))
      (then
        (loop $lp
          (if
            (i32.and (local.get 0) (i32.const 7))
            (then
              (if (i32.eqz (local.get 2)) (then return))
              (call $sideSetI8 (local.get 0) (call $mainGetI8 (local.get 1)))
              (local.set 0 (i32.add (local.get 0) (i32.const 1)))
              (local.set 1 (i32.add (local.get 1) (i32.const 1)))
              (local.set 2 (i32.sub (local.get 2) (i32.const 1)))
              (br $lp)
            )
          )
        )
        (loop $lp
          (if
            (i32.ge_s (local.get 2) (i32.const 8))
            (then
              (call $sideSetI64 (local.get 0) (call $mainGetI64 (local.get 1)))
              (local.set 0 (i32.add (local.get 0) (i32.const 8)))
              (local.set 1 (i32.add (local.get 1) (i32.const 8)))
              (local.set 2 (i32.sub (local.get 2) (i32.const 8)))
              (br $lp)
            )
          )
        )
      )
    )
    (loop $lp
      (if
        (i32.gt_s (local.get 2) (i32.const 0))
        (then
          (call $sideSetI8 (local.get 0) (call $mainGetI8 (local.get 1)))
          (local.set 0 (i32.add (local.get 0) (i32.const 1)))
          (local.set 1 (i32.add (local.get 1) (i32.const 1)))
          (local.set 2 (i32.sub (local.get 2) (i32.const 1)))
          (br $lp)
        )
      )
    )
  )

  (func $stm_memcpy (export "stm_memcpy") (type $memcpyType)
    ;; 0: dest, 1: src, 2: size
    (if
      (i32.eq
        (i32.and (local.get 0) (i32.const 7))
        (i32.and (local.get 1) (i32.const 7)))
      (then
        (loop $lp
          (if
            (i32.and (local.get 0) (i32.const 7))
            (then
              (if (i32.eqz (local.get 2)) (then return))
              (call $mainSetI8 (local.get 0) (call $sideGetI8 (local.get 1)))
              (local.set 0 (i32.add (local.get 0) (i32.const 1)))
              (local.set 1 (i32.add (local.get 1) (i32.const 1)))
              (local.set 2 (i32.sub (local.get 2) (i32.const 1)))
              (br $lp)
            )
          )
        )
        (loop $lp
          (if
            (i32.ge_s (local.get 2) (i32.const 8))
            (then
              (call $mainSetI64 (local.get 0) (call $sideGetI64 (local.get 1)))
              (local.set 0 (i32.add (local.get 0) (i32.const 8)))
              (local.set 1 (i32.add (local.get 1) (i32.const 8)))
              (local.set 2 (i32.sub (local.get 2) (i32.const 8)))
              (br $lp)
            )
          )
        )
      )
    )
    (loop $lp
      (if
        (i32.gt_s (local.get 2) (i32.const 0))
        (then
          (call $mainSetI8 (local.get 0) (call $sideGetI8 (local.get 1)))
          (local.set 0 (i32.add (local.get 0) (i32.const 1)))
          (local.set 1 (i32.add (local.get 1) (i32.const 1)))
          (local.set 2 (i32.sub (local.get 2) (i32.const 1)))
          (br $lp)
        )
      )
    )
  )
)
