(defm defn (name args body)
	(if (atom args)
	 `(set ,name (fn (,args) ,body))
	 `(set ,name (fn ,args ,body))))

(defn cadr x (car (cdr x)))

(defn cdar x (cdr (car x)))

(defn caar x (car (car x)))

(defn append ((body &rest))
  (letrec (rc) ((fn (a d)
		    (if (null a)
			(if (null d) nil
			  (if (null (car d)) (rc (cdar d) (cdr d))
			    (cons (caar d) (rc (cdar d) (cdr d)))))
		      (cons (car a) (rc (cdr a) d)))))
	  (rc (car body) (cdr body))))
