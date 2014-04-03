(defm defn (name args body)
	`(set ,name (fn ,args ,body)))