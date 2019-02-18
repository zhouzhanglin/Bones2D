using UnityEngine;

namespace Bones2D
{
	/// <summary>
	/// Flash Matrix 2D
	/// author:bingheliefeng
	/// </summary>
	public class BoneMatrix2D {

		public float a, b, c, d, tx, ty;
		const float DEG_TO_RAD = Mathf.PI/180f;
		const float PI = Mathf.PI;

		public BoneMatrix2D(float a=1f,float b=0f,float c=0f,float d=1f,float tx=0f,float ty=0f){
			this.a = a; this.b = b; this.c= c;this.d=d;this.tx = tx;this.ty=ty;
		}


		public BoneMatrix2D SetTo(float a,float b,float c,float d,float tx,float ty) {
			this.a = a; this.b = b; this.c= c;this.d=d;this.tx = tx;this.ty=ty;
			return this;
		}


		public BoneMatrix2D Concat(BoneMatrix2D other) {
			var a =  this.a * other.a;
			var b =  0.0f;
			var c =  0.0f;
			var d =  this.d * other.d;
			var tx = this.tx * other.a + other.tx;
			var ty = this.ty * other.d + other.ty;

			if (this.b != 0.0f || this.c != 0.0f || other.b != 0.0f || other.c != 0.0f) {
				a  += this.b * other.c;
				d  += this.c * other.b;
				b  += this.a * other.b + this.b * other.d;
				c  += this.c * other.a + this.d * other.c;
				tx += this.ty * other.c;
				ty += this.tx * other.b;
			}

			this.a = a;
			this.b = b;
			this.c = c;
			this.d = d;
			this.tx = tx;
			this.ty = ty;
			return this;
		}


		//angle为弧度
		public BoneMatrix2D Rotate(float angle) {
			if (angle != 0) {
				angle = angle / DEG_TO_RAD;
				var u = Mathf.Cos(angle);
				var v = Mathf.Sin(angle);
				var ta = this.a;
				var tb = this.b;
				var tc = this.c;
				var td = this.d;
				var ttx = this.tx;
				var tty = this.ty;
				this.a = ta  * u - tb  * v;
				this.b = ta  * v + tb  * u;
				this.c = tc  * u - td  * v;
				this.d = tc  * v + td  * u;
				this.tx = ttx * u - tty * v;
				this.ty = ttx * v + tty * u;
			}
			return this;
		}

		//0-360
		public float GetAngle(){
			var px = new Vector2(0, 1);  
			px = DeltaTransformPoint(px);  

			var angle = (180f/Mathf.PI) * Mathf.Atan2(px.y, px.x) - 90;  
			if(angle<0) angle+=360;

			return angle;
		}


		public BoneMatrix2D Prepend(float a,float b,float c,float d,float tx,float ty){
			var tx1 = this.tx;
			if (a != 1 || b != 0 || c != 0 || d != 1) {
				var a1 = this.a;
				var c1 = this.c;
				this.a = a1 * a + this.b * c;
				this.b = a1 * b + this.b * d;
				this.c = c1 * a + this.d * c;
				this.d = c1 * b + this.d * d;
			}
			this.tx = tx1 * a + this.ty * c + tx;
			this.ty = tx1 * b + this.ty * d + ty;
			return this;
		}
		public BoneMatrix2D Append(float a,float b,float c,float d,float tx,float ty){
			var a1 = this.a;
			var b1 = this.b;
			var c1 = this.c;
			var d1 = this.d;
			if (a != 1 || b != 0 || c != 0 || d != 1) {
				this.a = a * a1 + b * c1;
				this.b = a * b1 + b * d1;
				this.c = c * a1 + d * c1;
				this.d = c * b1 + d * d1;
			}
			this.tx = tx * a1 + ty * c1 + this.tx;
			this.ty = tx * b1 + ty * d1 + this.ty;
			return this;
		}


		public BoneMatrix2D Scale(float sx,float sy) {
			if (sx != 1) {
				this.a *= sx;
				this.c *= sx;
				this.tx *= sx;
			}
			if (sy != 1) {
				this.b *= sy;
				this.d *= sy;
				this.ty *= sy;
			}
			return this;
		}

		public float GetScaleX(){
			return Mathf.Sin(a)*Mathf.Sqrt(a*a+b*b); 
		}
		public float GetScaleY(){
			return Mathf.Sin(b)*Mathf.Sqrt(c*c+d*d); 
		}


		public BoneMatrix2D Translate(float dx,float dy) {
			this.tx += dx;
			this.ty += dy;
			return this;
		}

		public BoneMatrix2D Identity(){
			this.a = this.d = 1;
			this.b = this.c = this.tx = this.ty = 0;
			return this;
		}

		public BoneMatrix2D Invert(){
			return Invert(this);
		}
		public BoneMatrix2D Invert(BoneMatrix2D target){
			var a = this.a;
			var b  = this.b;
			var c  = this.c;
			var d = this.d;
			var tx = this.tx;
			var ty = this.ty;
			if (b == 0 && c == 0) {
				target.b = target.c = 0;
				if(a==0||d==0){
					target.a = target.d = target.tx = target.ty = 0;
				}
				else{
					a = target.a = 1 / a;
					d = target.d = 1 / d;
					target.tx = -a * tx;
					target.ty = -d * ty;
				}
				return this;
			}
			var determinant = a * d - b * c;
			if (determinant == 0) {
				target.Identity();
				return this;
			}
			determinant = 1 / determinant;
			var k = target.a =  d * determinant;
			b = target.b = -b * determinant;
			c = target.c = -c * determinant;
			d = target.d =  a * determinant;
			target.tx = -(k * tx + c * ty);
			target.ty = -(b * tx + d * ty);
			return this;
		}

		public Vector2 TransformPoint(float pointX,float pointY,ref Vector2 pt){
			var x = this.a * pointX + this.c * pointY + this.tx;
			var y = this.b * pointX + this.d * pointY + this.ty;

			pt.Set(x, y);
			return pt;
		}
		public Vector2 TransformPoint(float pointX,float pointY){
			var x = this.a * pointX + this.c * pointY + this.tx;
			var y = this.b * pointX + this.d * pointY + this.ty;

			return new Vector2(x,y);
		}

		public Vector2 DeltaTransformPoint(Vector2 point) {
			var x = this.a * point.x + this.c * point.y;
			var y = this.b * point.x + this.d * point.y;
			return new Vector2(x, y);
		}


		public BoneMatrix2D CreateBox(float scaleX, float scaleY,float rotation = 0,float tx = 0,float ty = 0) {
			if (rotation != 0) {
				rotation = rotation / DEG_TO_RAD;
				var u = Mathf.Cos(rotation);
				var v =Mathf.Sin(rotation);
				this.a = u * scaleX;
				this.b = v * scaleY;
				this.c = -v * scaleX;
				this.d = u * scaleY;
			} else {
				this.a = scaleX;
				this.b = 0;
				this.c = 0;
				this.d = scaleY;
			}
			this.tx = tx;
			this.ty = ty;
			return this;
		}

		public BoneMatrix2D CreateGradientBox(float width,float height,float rotation = 0,float tx = 0,float ty = 0) {
			return this.CreateBox(width / 1638.4f, height / 1638.4f, rotation, tx + width / 2f, ty + height / 2f);
		}

		public BoneMatrix2D Copy(BoneMatrix2D matrix) {
			return this.SetTo(matrix.a, matrix.b, matrix.c, matrix.d, matrix.tx, matrix.ty);
		}

		public BoneMatrix2D Clone() {
			return new BoneMatrix2D(this.a, this.b, this.c, this.d, this.tx, this.ty);
		}

		public override string ToString() {
			return "[Matrix2D (a="+this.a+" b="+this.b+" c="+this.c+" d="+this.d+" tx="+this.tx+" ty="+this.ty+")]";
		}
	}

}
