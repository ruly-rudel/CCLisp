(defm defn (name args body)
	(if (atom args)
	 `(set ,name (fn (,args) ,body))
	 `(set ,name (fn ,args ,body))))