;; mts -> main to side
;; stm -> side to main
(module
  (type $addFnPtrType (func (param $fPtr i32) (result i32)))
  (type $memcpyType (func (param $dest i32) (param $src i32) (param $size i32)))
  (type $fixedMemcpyType (func (param $dest i32) (param $src i32)))

  (import "main" "memory" (memory $memMain 1))
  (import "side" "memory" (memory $memSide 1))

  (import "mainToSide" "addFnPtr" (func $mts_addFnPtr (type $addFnPtrType)))
  (export "mts_addFnPtr" (func $mts_addFnPtr))

  (import "sideToMain" "addFnPtr" (func $stm_addFnPtr (type $addFnPtrType)))
  (export "stm_addFnPtr" (func $stm_addFnPtr))

  (func (export "mts_memcpy") (type $memcpyType)
    ;; 0: dest, 1: src, 2: size
    (memory.copy $memSide $memMain (local.get 0) (local.get 1) (local.get 2))
  )
  (func (export "mts_memcpy1") (type $fixedMemcpyType)
    (i32.store8 $memSide (local.get 0) (i32.load8_u $memMain (local.get 1)))
  )
  (func (export "mts_memcpy2") (type $fixedMemcpyType)
    (i32.store16 $memSide (local.get 0) (i32.load16_u $memMain (local.get 1)))
  )
  (func (export "mts_memcpy4") (type $fixedMemcpyType)
    (i32.store $memSide (local.get 0) (i32.load $memMain (local.get 1)))
  )
  (func (export "mts_memcpy8") (type $fixedMemcpyType)
    (i64.store $memSide (local.get 0) (i64.load $memMain (local.get 1)))
  )

  (func (export "stm_memcpy") (type $memcpyType)
    ;; 0: dest, 1: src, 2: size
    (memory.copy $memMain $memSide (local.get 0) (local.get 1) (local.get 2))
  )
  (func (export "stm_memcpy1") (type $fixedMemcpyType)
    (i32.store8 $memMain (local.get 0) (i32.load8_u $memSide (local.get 1)))
  )
  (func (export "stm_memcpy2") (type $fixedMemcpyType)
    (i32.store16 $memMain (local.get 0) (i32.load16_u $memSide (local.get 1)))
  )
  (func (export "stm_memcpy4") (type $fixedMemcpyType)
    (i32.store $memMain (local.get 0) (i32.load $memSide (local.get 1)))
  )
  (func (export "stm_memcpy8") (type $fixedMemcpyType)
    (i64.store $memMain (local.get 0) (i64.load $memSide (local.get 1)))
  )
)
