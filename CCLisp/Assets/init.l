

(set cadr (fn (x) (car (cdr x))))

(set cdar (fn (x) (cdr (car x))))

(set caar (fn (x) (car (car x))))

(set append (fn ((body &rest))
  (letrec (rc) ((fn (a d)
		    (if (null a)
			(if (null d) nil
			  (if (null (car d)) (rc (cdar d) (cdr d))
			    (cons (caar d) (rc (cdar d) (cdr d)))))
		      (cons (car a) (rc (cdr a) d)))))
	  (rc (car body) (cdr body)))))

(defm defn (name args body)
	(if (atom args)
	 `(set ,name (fn (,args) ,body))
	 `(set ,name (fn ,args ,body))))

