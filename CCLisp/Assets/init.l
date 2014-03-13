(defmacro defun (name args body)
	`(setf ,name (lambda ,args ,body)))