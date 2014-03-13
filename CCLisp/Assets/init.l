(defmacro defun (name args body)
  (cons 'setf (cons name (cons (cons 'lambda (cons args (cons body nil))) nil))))
