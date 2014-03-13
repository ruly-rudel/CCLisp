(defmacro defun (name args body)
  (cons (quote setf) (cons name (cons (cons (quote lambda) (cons args (cons body nil))) nil))))
